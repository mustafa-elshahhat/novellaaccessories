using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>
/// A discount coupon. Applies to the product subtotal only (never shipping).
/// Customer-specific coupons (e.g. the two-delivered-orders reward) are bound to a customer.
/// </summary>
public class Coupon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int? PerCustomerUsageLimit { get; set; }
    public decimal? MinimumOrderSubtotal { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCustomerSpecific { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public CouponSource Source { get; set; } = CouponSource.General;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}

/// <summary>Records a single use of a coupon by a customer on an order.</summary>
public class CouponUsage
{
    public Guid Id { get; set; }
    public Guid CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime UsedAt { get; set; }
}

/// <summary>Singleton settings controlling the two-delivered-orders reward coupon.</summary>
public class TwoOrderCouponSettings
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int ValidityDays { get; set; }
    public decimal? MinimumOrderSubtotal { get; set; }
    public bool SendWhatsAppMessage { get; set; }
    public DateTime UpdatedAt { get; set; }
}
