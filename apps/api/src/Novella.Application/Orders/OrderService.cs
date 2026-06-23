using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Orders;

/// <summary>
/// Order lifecycle + stock authority. Enforces valid status transitions, deducts stock exactly
/// once on Confirmed, restores it on eligible cancellation, records inventory movements and
/// timeline timestamps, and triggers the two-delivered-orders reward on Delivered.
/// </summary>
public sealed class OrderService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly TwoOrderCouponService _reward;
    private readonly WhatsAppMessenger _whatsApp;

    public OrderService(IAppDbContext db, IClock clock, TwoOrderCouponService reward, WhatsAppMessenger whatsApp)
    {
        _db = db;
        _clock = clock;
        _reward = reward;
        _whatsApp = whatsApp;
    }

    // ---------- Customer ----------

    public async Task<IReadOnlyList<CustomerOrderDto>> GetMyOrdersAsync(Guid customerId, CancellationToken ct)
    {
        await EnsureCustomerCanAccessOrdersAsync(customerId, ct);
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(MapCustomer).ToList();
    }

    public async Task<CustomerOrderDto> GetMyOrderAsync(Guid customerId, string orderNumber, CancellationToken ct)
    {
        await EnsureCustomerCanAccessOrdersAsync(customerId, ct);
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderNumber == orderNumber, ct)
            ?? throw AppException.NotFound("Order not found.");
        return MapCustomer(order!);
    }

    public async Task<CustomerOrderDto> CancelMyOrderAsync(Guid customerId, string orderNumber, CancelOrderRequest req, CancellationToken ct)
    {
        await EnsureCustomerCanAccessOrdersAsync(customerId, ct);
        Order? order = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async () =>
            {
                order = await _db.Orders.Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderNumber == orderNumber, ct)
                    ?? throw AppException.NotFound("Order not found.");

                if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
                    throw new AppException(ErrorCodes.OrderCannotBeCancelled,
                        "Order can only be cancelled while Pending or Confirmed.", 409);

                await CancelInternalAsync(order, req.Reason ?? "Cancelled by customer", ct);
                await _db.SaveChangesAsync(ct);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ErrorCodes.ConcurrencyConflict, "Order was changed by another operation. Refresh and try again.", 409);
        }
        return MapCustomer(order!);
    }

    // ---------- Admin ----------

    public async Task<PagedResult<AdminOrderListItemDto>> GetAdminOrdersAsync(AdminOrderListQuery query, CancellationToken ct)
    {
        var q = _db.Orders.AsNoTracking().AsQueryable();
        if (query.Status is { } st) q = q.Where(o => o.Status == st);
        if (query.PaymentMethod is { } pm) q = q.Where(o => o.PaymentMethod == pm);
        if (!string.IsNullOrWhiteSpace(query.Governorate))
        {
            var governorate = query.Governorate.Trim();
            q = q.Where(o => o.GovernorateNameAr.Contains(governorate) || o.GovernorateNameEn.Contains(governorate));
        }
        if (query.From is { } from) q = q.Where(o => o.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(o => o.CreatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || o.CustomerName.Contains(s) || o.CustomerPhone.Contains(s));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(o => new AdminOrderListItemDto(o.Id, o.OrderNumber, o.Status, o.CustomerName, o.CustomerPhone,
                o.GrandTotal, o.PaymentMethod, o.PaymentStatus, o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AdminOrderListItemDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<AdminOrderDto> GetAdminOrderAsync(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("Order not found.");
        return MapAdmin(order);
    }

    public async Task<AdminOrderDto> UpdateStatusAsync(Guid id, OrderStatus newStatus, CancellationToken ct)
    {
        Order? order = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async () =>
            {
                order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw AppException.NotFound("Order not found.");

                if (newStatus == OrderStatus.Cancelled)
                {
                    await CancelByAdminAsync(order, "Cancelled by admin", ct);
                    await _db.SaveChangesAsync(ct);
                    return;
                }

                if (!IsValidForwardTransition(order.Status, newStatus))
                    throw new AppException(ErrorCodes.OrderInvalidTransition,
                        $"Cannot move order from {order.Status} to {newStatus}.", 409);

                var now = _clock.UtcNow;
                switch (newStatus)
                {
                    case OrderStatus.Confirmed:
                        order.Status = OrderStatus.Confirmed;
                        order.ConfirmedAt = now;
                        await DeductStockAsync(order, ct);
                        await SendOrderConfirmationAsync(order, ct);
                        break;
                    case OrderStatus.Preparing:
                        order.Status = OrderStatus.Preparing;
                        order.PreparingAt = now;
                        break;
                    case OrderStatus.Shipped:
                        order.Status = OrderStatus.Shipped;
                        order.ShippedAt = now;
                        break;
                    case OrderStatus.Delivered:
                        order.Status = OrderStatus.Delivered;
                        order.DeliveredAt = now;
                        order.PaymentStatus = order.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Paid : order.PaymentStatus;
                        break;
                }
                order.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ErrorCodes.ConcurrencyConflict, "Order was changed by another operation. Refresh and try again.", 409);
        }

        if (newStatus == OrderStatus.Delivered)
            await _reward.EvaluateOnDeliveredAsync(order!.CustomerId, ct);

        return MapAdmin(order!);
    }

    public async Task<AdminOrderDto> CancelByAdminEndpointAsync(Guid id, CancelOrderRequest req, CancellationToken ct)
    {
        Order? order = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async () =>
            {
                order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw AppException.NotFound("Order not found.");
                await CancelByAdminAsync(order, req.Reason ?? "Cancelled by admin", ct);
                await _db.SaveChangesAsync(ct);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(ErrorCodes.ConcurrencyConflict, "Order was changed by another operation. Refresh and try again.", 409);
        }
        return MapAdmin(order!);
    }

    public async Task<AdminOrderDto> UpdateShippingAsync(Guid id, UpdateShippingRequest req, CancellationToken ct)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw AppException.NotFound("Order not found.");
        order.ShippingProviderName = req.ShippingProviderName;
        order.ExternalTrackingNumber = req.ExternalTrackingNumber;
        order.ExternalShippingStatus = req.ExternalShippingStatus;
        order.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapAdmin(order);
    }

    // ---------- internal ----------

    private static bool IsValidForwardTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
        (OrderStatus.Preparing, OrderStatus.Shipped) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        _ => false
    };

    private async Task CancelByAdminAsync(Order order, string reason, CancellationToken ct)
    {
        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new AppException(ErrorCodes.OrderCannotBeCancelled, "Order is already terminal.", 409);
        await CancelInternalAsync(order, reason, ct);
    }

    private async Task CancelInternalAsync(Order order, string reason, CancellationToken ct)
    {
        // Restore stock if it was deducted (Confirmed+ before fulfillment).
        if (order.StockDeducted)
            await RestoreStockAsync(order, ct);

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = _clock.UtcNow;
        order.CancellationReason = reason;
        order.PaymentStatus = PaymentStatus.Cancelled;
        order.UpdatedAt = _clock.UtcNow;
    }

    private async Task DeductStockAsync(Order order, CancellationToken ct)
    {
        if (order.StockDeducted) return; // exactly once

        var variantIds = order.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await _db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        foreach (var item in order.Items)
        {
            if (!variants.TryGetValue(item.ProductVariantId, out var variant)) continue;
            if (variant.StockQuantity < item.Quantity)
                throw new AppException(ErrorCodes.VariantOutOfStock, "A selected variant no longer has enough stock.", 409);
            variant.StockQuantity -= item.Quantity;
            variant.UpdatedAt = _clock.UtcNow;
            _db.InventoryMovements.Add(new InventoryMovement
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                OrderId = order.Id,
                MovementType = MovementType.Deduct,
                Quantity = item.Quantity,
                Reason = $"Order {order.OrderNumber} confirmed",
                CreatedAt = _clock.UtcNow
            });
        }
        order.StockDeducted = true;
    }

    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        if (!order.StockDeducted) return;

        var variantIds = order.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await _db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        foreach (var item in order.Items)
        {
            if (!variants.TryGetValue(item.ProductVariantId, out var variant)) continue;
            variant.StockQuantity += item.Quantity;
            variant.UpdatedAt = _clock.UtcNow;
            _db.InventoryMovements.Add(new InventoryMovement
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                OrderId = order.Id,
                MovementType = MovementType.Restore,
                Quantity = item.Quantity,
                Reason = $"Order {order.OrderNumber} cancelled",
                CreatedAt = _clock.UtcNow
            });
        }
        order.StockDeducted = false;
    }

    private async Task SendOrderConfirmationAsync(Order order, CancellationToken ct)
    {
        var body = TemplateRenderer.Render(DefaultTemplates.OrderConfirmation, new Dictionary<string, string>
        {
            ["name"] = order.CustomerName,
            ["order_number"] = order.OrderNumber,
            ["total"] = order.GrandTotal.ToString("0.00"),
            ["payment_method"] = order.PaymentMethod.ToString()
        });
        await _whatsApp.SendAsync(WhatsAppMessageType.OrderConfirmation, "order_confirmation", order.CustomerPhone, order.CustomerId, body, ct);
    }

    private async Task EnsureCustomerCanAccessOrdersAsync(Guid customerId, CancellationToken ct)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw AppException.NotFound("Customer not found.");
        if (!customer.IsActive)
            throw AppException.Forbidden("Customer account is inactive.");
        if (!customer.IsPhoneVerified)
            throw new AppException(ErrorCodes.PhoneNotVerified, "Phone number is not verified.", 403);
    }

    private static CustomerOrderDto MapCustomer(Order o) => new(
        o.OrderNumber, o.Status, o.CustomerName, o.CustomerPhone,
        o.GovernorateNameAr, o.GovernorateNameEn, o.CityDistrict, o.DetailedAddress, o.Notes,
        o.ProductSubtotalBeforeDiscount, o.ProductDiscountTotal, o.CouponDiscountTotal, o.ProductSubtotalAfterDiscount,
        o.CustomerPaidShippingFee, o.GrandTotal,
        o.PaymentMethod, o.PaymentStatus, o.CouponCode,
        o.CreatedAt, o.ConfirmedAt, o.PreparingAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt,
        o.ExternalTrackingNumber,
        o.Items.Select(i => new CustomerOrderItemDto(i.ProductNameAr, i.ProductNameEn, i.VariantNameAr, i.VariantNameEn, i.Sku,
            i.Quantity, i.OriginalUnitSellingPrice, i.FinalUnitPrice, i.LineRevenue)).ToList());

    private static AdminOrderDto MapAdmin(Order o) => new(
        o.Id, o.OrderNumber, o.Status, o.CustomerId, o.CustomerName, o.CustomerPhone,
        o.GovernorateNameAr, o.GovernorateNameEn, o.CityDistrict, o.DetailedAddress, o.Notes,
        o.ProductSubtotalBeforeDiscount, o.ProductDiscountTotal, o.CouponDiscountTotal, o.ProductSubtotalAfterDiscount,
        o.CustomerPaidShippingFee, o.ActualShippingCost, o.ShippingMargin, o.GrandTotal,
        o.PaymentMethod, o.PaymentStatus, o.CouponCode,
        o.ShippingProviderName, o.ExternalTrackingNumber, o.ExternalShippingStatus,
        o.CreatedAt, o.ConfirmedAt, o.PreparingAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt, o.CancellationReason,
        o.Items.Select(i => new AdminOrderItemDto(i.ProductNameAr, i.ProductNameEn, i.Sku, i.Quantity,
            i.OriginalUnitSellingPrice, i.ProductDiscountAmountPerUnit, i.UnitPriceAfterProductDiscount,
            i.CouponDiscountAmountPerUnit, i.FinalUnitPrice, i.PurchaseCostPerUnit, i.LineRevenue, i.LineCost, i.LineGrossProfit)).ToList());
}
