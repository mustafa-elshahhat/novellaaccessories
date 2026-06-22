using Novella.Domain.Enums;

namespace Novella.Application.Checkout;

public sealed record CheckoutPreviewRequest(Guid GovernorateId, string? CouponCode);

public sealed record CreateOrderRequest(
    Guid GovernorateId, string CityDistrict, string DetailedAddress, string? Notes,
    PaymentMethod PaymentMethod, string? CouponCode, string? IdempotencyKey = null);

/// <summary>Result of order creation. Returns both the id and the customer-facing order number.</summary>
public sealed record CreateOrderResult(Guid OrderId, string OrderNumber);

public sealed record CheckoutLineDto(
    Guid ProductVariantId, string ProductNameAr, string ProductNameEn, string Sku, int Quantity,
    decimal OriginalUnitPrice, decimal FinalUnitPrice, decimal LineTotal);

/// <summary>Customer-facing totals. NEVER includes actual shipping cost or profit.</summary>
public sealed record CheckoutPreviewDto(
    IReadOnlyList<CheckoutLineDto> Items,
    decimal ProductSubtotalBeforeDiscount,
    decimal ProductDiscountTotal,
    decimal CouponDiscountTotal,
    decimal ProductSubtotalAfterDiscount,
    decimal ShippingFee,
    decimal GrandTotal,
    string? AppliedCouponCode,
    bool CouponApplied,
    IReadOnlyList<string> Warnings);
