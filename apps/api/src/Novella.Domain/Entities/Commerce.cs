using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>
/// Per-governorate shipping configuration. <see cref="ActualShippingCost"/> is admin-only
/// and must never be exposed to customers.
/// </summary>
public class ShippingGovernorate
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal CustomerPaidShippingFee { get; set; }
    /// <summary>Admin-only actual shipping cost. NEVER exposed to customers.</summary>
    public decimal ActualShippingCost { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>A payment transaction tied to an order (COD active; gateways prepared).</summary>
public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ProviderName { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string? ProviderTransactionReference { get; set; }
    public string? ProviderResponse { get; set; }
    public decimal? CommissionAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>A business expense feeding net-profit reporting.</summary>
public class Expense
{
    public Guid Id { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Notes { get; set; }
    public Guid? RelatedOrderId { get; set; }
    public string? RelatedCampaignName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
