using Novella.Application.Common;
using Novella.Domain.Enums;

namespace Novella.Application.Orders;

// ---------- Customer-facing (NO cost / profit / actual shipping cost) ----------

public sealed record CustomerOrderItemDto(
    string ProductNameAr, string ProductNameEn, string? VariantNameAr, string? VariantNameEn, string Sku,
    int Quantity, decimal OriginalUnitPrice, decimal FinalUnitPrice, decimal LineTotal);

public sealed record CustomerOrderDto(
    string OrderNumber, OrderStatus Status,
    string CustomerName, string CustomerPhone,
    string GovernorateNameAr, string GovernorateNameEn, string CityDistrict, string DetailedAddress, string? Notes,
    decimal ProductSubtotalBeforeDiscount, decimal ProductDiscountTotal, decimal CouponDiscountTotal,
    decimal ProductSubtotalAfterDiscount, decimal ShippingFee, decimal GrandTotal,
    PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, string? CouponCode,
    DateTime CreatedAt, DateTime? ConfirmedAt, DateTime? PreparingAt, DateTime? ShippedAt, DateTime? DeliveredAt, DateTime? CancelledAt,
    string? TrackingNumber,
    IReadOnlyList<CustomerOrderItemDto> Items);

public sealed record CancelOrderRequest(string? Reason);

// ---------- Admin (includes cost, profit, actual shipping cost) ----------

public sealed record AdminOrderItemDto(
    string ProductNameAr, string ProductNameEn, string Sku, int Quantity,
    decimal OriginalUnitSellingPrice, decimal ProductDiscountAmountPerUnit, decimal UnitPriceAfterProductDiscount,
    decimal CouponDiscountAmountPerUnit, decimal FinalUnitPrice,
    decimal PurchaseCostPerUnit, decimal LineRevenue, decimal LineCost, decimal LineGrossProfit);

public sealed record AdminOrderDto(
    Guid Id, string OrderNumber, OrderStatus Status, Guid CustomerId,
    string CustomerName, string CustomerPhone,
    string GovernorateNameAr, string GovernorateNameEn, string CityDistrict, string DetailedAddress, string? Notes,
    decimal ProductSubtotalBeforeDiscount, decimal ProductDiscountTotal, decimal CouponDiscountTotal,
    decimal ProductSubtotalAfterDiscount,
    decimal CustomerPaidShippingFee, decimal ActualShippingCost, decimal ShippingMargin, decimal GrandTotal,
    PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, string? CouponCode,
    string? ShippingProviderName, string? ExternalTrackingNumber, string? ExternalShippingStatus,
    DateTime CreatedAt, DateTime? ConfirmedAt, DateTime? PreparingAt, DateTime? ShippedAt, DateTime? DeliveredAt, DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<AdminOrderItemDto> Items);

public sealed record AdminOrderListItemDto(
    Guid Id, string OrderNumber, OrderStatus Status, string CustomerName, string CustomerPhone,
    decimal GrandTotal, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, DateTime CreatedAt);

public sealed record UpdateOrderStatusRequest(OrderStatus Status);
public sealed record UpdateShippingRequest(string? ShippingProviderName, string? ExternalTrackingNumber, string? ExternalShippingStatus);

public sealed class AdminOrderListQuery : PageQuery
{
    public OrderStatus? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
