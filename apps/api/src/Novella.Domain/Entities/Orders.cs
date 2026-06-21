using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>
/// A customer order with full price + shipping + address snapshots taken at creation time.
/// Cost/profit fields are admin-only and must never be exposed to customers.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public Guid GovernorateId { get; set; }
    public string GovernorateNameAr { get; set; } = string.Empty;
    public string GovernorateNameEn { get; set; } = string.Empty;
    public string CityDistrict { get; set; } = string.Empty;
    public string DetailedAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public decimal ProductSubtotalBeforeDiscount { get; set; }
    public decimal ProductDiscountTotal { get; set; }
    public decimal CouponDiscountTotal { get; set; }
    public decimal ProductSubtotalAfterDiscount { get; set; }
    public decimal CustomerPaidShippingFee { get; set; }
    /// <summary>Admin-only actual shipping cost. NEVER exposed to customers.</summary>
    public decimal ActualShippingCost { get; set; }
    /// <summary>Admin-only shipping margin (paid - actual). NEVER exposed to customers.</summary>
    public decimal ShippingMargin { get; set; }
    public decimal GrandTotal { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public Guid? CouponId { get; set; }
    public string? CouponCode { get; set; }

    public string? ShippingProviderName { get; set; }
    public string? ExternalTrackingNumber { get; set; }
    public string? ExternalShippingStatus { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public bool StockDeducted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

/// <summary>
/// A frozen snapshot of a purchased line. Stores the full pricing breakdown plus admin-only
/// cost/profit fields used by reports. Cost/profit fields must never reach customers.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductVariantId { get; set; }

    public string ProductNameAr { get; set; } = string.Empty;
    public string ProductNameEn { get; set; } = string.Empty;
    public string? VariantNameAr { get; set; }
    public string? VariantNameEn { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public decimal OriginalUnitSellingPrice { get; set; }
    public decimal? ProductDiscountPercentage { get; set; }
    public decimal ProductDiscountAmountPerUnit { get; set; }
    public decimal UnitPriceAfterProductDiscount { get; set; }
    public decimal CouponDiscountAmountPerUnit { get; set; }
    public decimal FinalUnitPrice { get; set; }

    /// <summary>Admin-only per-unit purchase cost snapshot. NEVER exposed to customers.</summary>
    public decimal PurchaseCostPerUnit { get; set; }
    public decimal LineRevenue { get; set; }
    /// <summary>Admin-only line cost. NEVER exposed to customers.</summary>
    public decimal LineCost { get; set; }
    /// <summary>Admin-only line gross profit. NEVER exposed to customers.</summary>
    public decimal LineGrossProfit { get; set; }
}
