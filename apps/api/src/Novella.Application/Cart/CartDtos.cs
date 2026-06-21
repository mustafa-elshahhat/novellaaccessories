namespace Novella.Application.Cart;

public sealed record AddCartItemRequest(Guid ProductVariantId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);

/// <summary>Cart line for the customer. Exposes prices and availability only — never stock counts.</summary>
public sealed record CartItemDto(
    Guid ItemId, Guid ProductId, Guid ProductVariantId,
    string ProductNameAr, string ProductNameEn, string? VariantNameAr, string? VariantNameEn, string Sku,
    int Quantity, decimal OriginalUnitPrice, decimal UnitPrice, decimal LineTotal,
    bool IsAvailable, bool QuantityAdjusted);

public sealed record CartDto(
    Guid Id, IReadOnlyList<CartItemDto> Items,
    decimal ProductSubtotalBeforeDiscount, decimal ProductDiscountTotal, decimal SubtotalAfterProductDiscount,
    bool HasUnavailableItems);
