using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Application.Discounts;
using Novella.Application.Shipping;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.Checkout;

/// <summary>
/// Checkout preview and order creation. Both recompute everything server-side using the same
/// pricing engine so the preview total always equals the created order total. Created orders are
/// Pending with full price/shipping/address snapshots; stock is not deducted until Confirmed.
/// </summary>
public sealed class CheckoutService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PricingAssembler _assembler;
    private readonly CouponService _coupons;
    private readonly ShippingService _shipping;

    public CheckoutService(IAppDbContext db, IClock clock, PricingAssembler assembler, CouponService coupons, ShippingService shipping)
    {
        _db = db;
        _clock = clock;
        _assembler = assembler;
        _coupons = coupons;
        _shipping = shipping;
    }

    public async Task<CheckoutPreviewDto> PreviewAsync(Guid customerId, CheckoutPreviewRequest req, CancellationToken ct)
    {
        var computed = await ComputeAsync(customerId, req.GovernorateId, req.CouponCode, ct);
        return computed.ToPreview();
    }

    public async Task<CreateOrderResult> CreateOrderAsync(Guid customerId, CreateOrderRequest req, CancellationToken ct)
    {
        var idempotencyKey = NormalizeIdempotencyKey(req.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var existing = await _db.Orders.AsNoTracking()
                .Where(o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey)
                .Select(o => new CreateOrderResult(o.Id, o.OrderNumber))
                .FirstOrDefaultAsync(ct);
            if (existing is not null) return existing;
        }

        if (string.IsNullOrWhiteSpace(req.CityDistrict) || string.IsNullOrWhiteSpace(req.DetailedAddress))
            throw AppException.Validation("City/district and detailed address are required.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw AppException.NotFound("Customer not found.");

        // COD active; other methods rejected here (prepared but inactive).
        if (req.PaymentMethod != PaymentMethod.CashOnDelivery)
            throw new AppException(ErrorCodes.PaymentProviderNotActive,
                $"Payment method '{req.PaymentMethod}' is not active yet.", 409);

        CreateOrderResult? result = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async () =>
        {
            if (idempotencyKey is not null)
            {
                var duplicate = await _db.Orders.AsNoTracking()
                    .Where(o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey)
                    .Select(o => new CreateOrderResult(o.Id, o.OrderNumber))
                    .FirstOrDefaultAsync(ct);
                if (duplicate is not null)
                {
                    result = duplicate;
                    return;
                }
            }

            var computed = await ComputeAsync(customerId, req.GovernorateId, req.CouponCode, ct);
            var now = _clock.UtcNow;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = await GenerateOrderNumberAsync(ct),
                IdempotencyKey = idempotencyKey,
                CustomerId = customerId,
                Status = OrderStatus.Pending,
                CustomerName = customer.FullName,
                CustomerPhone = customer.PhoneNumber, // from the verified account, never the client
                GovernorateId = computed.Governorate.Id,
                GovernorateNameAr = computed.Governorate.NameAr,
                GovernorateNameEn = computed.Governorate.NameEn,
                CityDistrict = req.CityDistrict,
                DetailedAddress = req.DetailedAddress,
                Notes = req.Notes,
                ProductSubtotalBeforeDiscount = computed.Pricing.ProductSubtotalBeforeDiscount,
                ProductDiscountTotal = computed.Pricing.ProductDiscountTotal,
                CouponDiscountTotal = computed.Pricing.CouponDiscountTotal,
                ProductSubtotalAfterDiscount = computed.Pricing.ProductSubtotalAfterAllDiscounts,
                CustomerPaidShippingFee = computed.ShippingFee,
                ActualShippingCost = computed.Governorate.ActualShippingCost,
                ShippingMargin = computed.ShippingFee - computed.Governorate.ActualShippingCost,
                GrandTotal = computed.GrandTotal,
                PaymentMethod = req.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                CouponId = computed.Coupon?.Id,
                CouponCode = computed.Coupon?.Code,
                CreatedAt = now
            };

            foreach (var line in computed.Pricing.Lines)
            {
                var resolved = computed.Resolved.First(r => r.Variant.Id == line.ProductVariantId);
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    ProductVariantId = line.ProductVariantId,
                    ProductNameAr = resolved.Product.NameAr,
                    ProductNameEn = resolved.Product.NameEn,
                    VariantNameAr = resolved.Variant.NameAr,
                    VariantNameEn = resolved.Variant.NameEn,
                    Sku = resolved.Variant.Sku,
                    Quantity = line.Quantity,
                    OriginalUnitSellingPrice = line.OriginalUnitSellingPrice,
                    ProductDiscountPercentage = line.ProductDiscountPercentage,
                    ProductDiscountAmountPerUnit = line.ProductDiscountAmountPerUnit,
                    UnitPriceAfterProductDiscount = line.UnitPriceAfterProductDiscount,
                    CouponDiscountAmountPerUnit = line.CouponDiscountAmountPerUnit,
                    FinalUnitPrice = line.FinalUnitPrice,
                    PurchaseCostPerUnit = line.PurchaseCostPerUnit,
                    LineRevenue = line.LineRevenue,
                    LineCost = line.LineCost,
                    LineGrossProfit = line.LineGrossProfit
                });
            }

            _db.Orders.Add(order);
            if (computed.Coupon is not null)
                _coupons.RecordUsage(computed.Coupon.Id, customerId, order.Id, computed.Pricing.CouponDiscountTotal);

            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentMethod = req.PaymentMethod,
                ProviderName = "CashOnDelivery",
                Status = PaymentStatus.Pending,
                Amount = order.GrandTotal,
                CreatedAt = now
            });

            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
            if (cart is not null)
            {
                _db.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = now;
            }

            await _db.SaveChangesAsync(ct);
            result = new CreateOrderResult(order.Id, order.OrderNumber);
            }, ct);
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            var existing = await _db.Orders.AsNoTracking()
                .Where(o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey)
                .Select(o => new CreateOrderResult(o.Id, o.OrderNumber))
                .FirstOrDefaultAsync(ct);
            if (existing is not null) return existing;
            throw;
        }

        return result!;
    }

    private sealed class ComputedCheckout
    {
        public required IReadOnlyList<ResolvedLine> Resolved { get; init; }
        public required PricingResult Pricing { get; init; }
        public required ShippingGovernorate Governorate { get; init; }
        public required decimal ShippingFee { get; init; }
        public required decimal GrandTotal { get; init; }
        public Coupon? Coupon { get; init; }
        public string? CouponCode { get; init; }

        public CheckoutPreviewDto ToPreview()
        {
            var items = Pricing.Lines.Select(l =>
            {
                var r = Resolved.First(x => x.Variant.Id == l.ProductVariantId);
                return new CheckoutLineDto(l.ProductVariantId, r.Product.NameAr, r.Product.NameEn, r.Variant.Sku,
                    l.Quantity, l.OriginalUnitSellingPrice, l.FinalUnitPrice, l.LineRevenue);
            }).ToList();

            return new CheckoutPreviewDto(
                items,
                Pricing.ProductSubtotalBeforeDiscount,
                Pricing.ProductDiscountTotal,
                Pricing.CouponDiscountTotal,
                Pricing.ProductSubtotalAfterAllDiscounts,
                ShippingFee,
                GrandTotal,
                Coupon?.Code,
                Coupon is not null,
                Array.Empty<string>());
        }
    }

    private async Task<ComputedCheckout> ComputeAsync(Guid customerId, Guid governorateId, string? couponCode, CancellationToken ct)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (cart is null || cart.Items.Count == 0)
            throw AppException.Validation("Cart is empty.");

        var governorate = await _shipping.ResolveActiveAsync(governorateId, ct);

        var lineRequests = cart.Items.Select(i => new LineRequest(i.ProductVariantId, i.Quantity)).ToList();
        var resolved = await _assembler.ResolveAsync(lineRequests, strict: true, ct);
        var inputs = PricingAssembler.ToPricingInputs(resolved, useEffectiveQuantity: false);

        var now = _clock.UtcNow;

        // First pass without coupon to obtain the subtotal used for coupon validation.
        var baseline = PricingCalculator.Calculate(inputs, null, now);

        Coupon? coupon = null;
        CouponInput? couponInput = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var validation = await _coupons.ValidateAsync(couponCode, customerId, baseline.SubtotalAfterProductDiscount, ct);
            coupon = validation.Coupon;
            couponInput = validation.Input;
        }

        var pricing = couponInput is null ? baseline : PricingCalculator.Calculate(inputs, couponInput, now);

        // Free-shipping threshold (optional setting).
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var shippingFee = governorate.CustomerPaidShippingFee;
        if (settings is { IsFreeShippingEnabled: true, FreeShippingThreshold: { } threshold }
            && pricing.ProductSubtotalAfterAllDiscounts >= threshold)
            shippingFee = 0m;

        var grandTotal = PricingCalculator.Round(pricing.ProductSubtotalAfterAllDiscounts + shippingFee);

        return new ComputedCheckout
        {
            Resolved = resolved,
            Pricing = pricing,
            Governorate = governorate,
            ShippingFee = shippingFee,
            GrandTotal = grandTotal,
            Coupon = coupon,
            CouponCode = coupon?.Code
        };
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"NV-{_clock.UtcNow:yyyyMMdd}-{Random.Shared.Next(0, 1_000_000):D6}";
            if (!await _db.Orders.AnyAsync(o => o.OrderNumber == candidate, ct))
                return candidate;
        }
        return $"NV-{_clock.UtcNow:yyyyMMddHHmmssfff}";
    }

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > 128)
            throw AppException.Validation("Idempotency key is too long.");
        return trimmed;
    }
}
