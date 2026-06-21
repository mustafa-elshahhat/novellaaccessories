using Novella.Domain.Enums;

namespace Novella.Application.Discounts;

public sealed record AdminCouponDto(
    Guid Id, string Code, CouponType Type, decimal Value,
    DateTime? StartAt, DateTime? EndAt, int? TotalUsageLimit, int? PerCustomerUsageLimit,
    decimal? MinimumOrderSubtotal, bool IsActive, bool IsCustomerSpecific, Guid? CustomerId,
    CouponSource Source, int TimesUsed);

public sealed record CouponUpsertRequest(
    string Code, CouponType Type, decimal Value,
    DateTime? StartAt, DateTime? EndAt, int? TotalUsageLimit, int? PerCustomerUsageLimit,
    decimal? MinimumOrderSubtotal, bool IsActive, bool IsCustomerSpecific, Guid? CustomerId);

public sealed record CouponUsageDto(Guid Id, Guid CouponId, Guid CustomerId, Guid OrderId, decimal DiscountAmount, DateTime UsedAt);

public sealed record TwoOrderSettingsDto(
    bool IsEnabled, decimal DiscountPercentage, int ValidityDays, decimal? MinimumOrderSubtotal, bool SendWhatsAppMessage);
