using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.Discounts;

/// <summary>Result of validating a coupon against a customer + product subtotal.</summary>
public sealed record CouponValidation(Coupon Coupon, CouponInput Input);

/// <summary>
/// Coupon validation (existence, active, date window, usage limits, per-customer limit,
/// minimum subtotal, customer-specific ownership, single-use reward coupons) and admin CRUD.
/// Coupons apply to the product subtotal only — never shipping.
/// </summary>
public sealed class CouponService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CouponService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Validates a coupon for use. Throws an AppException with a specific code on failure.</summary>
    public async Task<CouponValidation> ValidateAsync(string code, Guid customerId, decimal productSubtotalAfterProductDiscount, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var normalized = code.Trim();

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == normalized, ct)
            ?? throw new AppException(ErrorCodes.CouponInvalid, "Coupon is invalid.", 400);

        if (!coupon.IsActive)
            throw new AppException(ErrorCodes.CouponInvalid, "Coupon is not active.", 400);

        if (coupon.StartAt is { } start && now < start)
            throw new AppException(ErrorCodes.CouponInvalid, "Coupon is not yet valid.", 400);

        if (coupon.EndAt is { } end && now > end)
            throw new AppException(ErrorCodes.CouponExpired, "Coupon is expired.", 400);

        if (coupon.IsCustomerSpecific && coupon.CustomerId != customerId)
            throw new AppException(ErrorCodes.CouponInvalid, "Coupon cannot be used by this customer.", 400);

        if (coupon.MinimumOrderSubtotal is { } min && productSubtotalAfterProductDiscount < min)
            throw new AppException(ErrorCodes.CouponMinSubtotal, "Order subtotal is below the coupon minimum.", 400,
                new Dictionary<string, object?> { ["minimumSubtotal"] = min });

        if (coupon.TotalUsageLimit is { } totalLimit)
        {
            var totalUsed = await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id, ct);
            if (totalUsed >= totalLimit)
                throw new AppException(ErrorCodes.CouponUsageLimitReached, "Coupon usage limit reached.", 400);
        }

        if (coupon.PerCustomerUsageLimit is { } perLimit)
        {
            var customerUsed = await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id && u.CustomerId == customerId, ct);
            if (customerUsed >= perLimit)
                throw new AppException(ErrorCodes.CouponUsageLimitReached, "You have already used this coupon.", 400);
        }

        return new CouponValidation(coupon, new CouponInput(coupon.Type, coupon.Value));
    }

    /// <summary>Records a coupon usage row (called inside the order-creation transaction).</summary>
    public void RecordUsage(Guid couponId, Guid customerId, Guid orderId, decimal discountAmount)
        => _db.CouponUsages.Add(new CouponUsage
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            CustomerId = customerId,
            OrderId = orderId,
            DiscountAmount = discountAmount,
            UsedAt = _clock.UtcNow
        });

    // ---------- Admin CRUD ----------

    public async Task<IReadOnlyList<AdminCouponDto>> ListAsync(CancellationToken ct)
        => await _db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminCouponDto(c.Id, c.Code, c.Type, c.Value, c.StartAt, c.EndAt,
                c.TotalUsageLimit, c.PerCustomerUsageLimit, c.MinimumOrderSubtotal, c.IsActive,
                c.IsCustomerSpecific, c.CustomerId, c.Source, c.Usages.Count))
            .ToListAsync(ct);

    public async Task<AdminCouponDto> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Coupons.AsNoTracking().Include(x => x.Usages).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Coupon not found.");
        return Map(c, c.Usages.Count);
    }

    public async Task<AdminCouponDto> CreateAsync(CouponUpsertRequest req, CancellationToken ct)
    {
        ValidateUpsert(req);
        var code = req.Code.Trim();
        if (await _db.Coupons.AnyAsync(c => c.Code == code, ct))
            throw AppException.Conflict("Coupon code already exists.");

        var c = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code,
            Type = req.Type,
            Value = req.Value,
            StartAt = req.StartAt,
            EndAt = req.EndAt,
            TotalUsageLimit = req.TotalUsageLimit,
            PerCustomerUsageLimit = req.PerCustomerUsageLimit,
            MinimumOrderSubtotal = req.MinimumOrderSubtotal,
            IsActive = req.IsActive,
            IsCustomerSpecific = req.IsCustomerSpecific,
            CustomerId = req.IsCustomerSpecific ? req.CustomerId : null,
            Source = CouponSource.General,
            CreatedAt = _clock.UtcNow
        };
        _db.Coupons.Add(c);
        await _db.SaveChangesAsync(ct);
        return Map(c, 0);
    }

    public async Task<AdminCouponDto> UpdateAsync(Guid id, CouponUpsertRequest req, CancellationToken ct)
    {
        ValidateUpsert(req);
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Coupon not found.");
        var code = req.Code.Trim();
        if (c.Code != code && await _db.Coupons.AnyAsync(x => x.Code == code, ct))
            throw AppException.Conflict("Coupon code already exists.");

        c.Code = code; c.Type = req.Type; c.Value = req.Value;
        c.StartAt = req.StartAt; c.EndAt = req.EndAt;
        c.TotalUsageLimit = req.TotalUsageLimit; c.PerCustomerUsageLimit = req.PerCustomerUsageLimit;
        c.MinimumOrderSubtotal = req.MinimumOrderSubtotal; c.IsActive = req.IsActive;
        c.IsCustomerSpecific = req.IsCustomerSpecific;
        c.CustomerId = req.IsCustomerSpecific ? req.CustomerId : null;
        c.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        var used = await _db.CouponUsages.CountAsync(u => u.CouponId == id, ct);
        return Map(c, used);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Coupon not found.");
        if (await _db.CouponUsages.AnyAsync(u => u.CouponId == id, ct))
        {
            c.IsActive = false; c.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }
        _db.Coupons.Remove(c);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var c = await _db.Coupons.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Coupon not found.");
        c.IsActive = isActive; c.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CouponUsageDto>> GetUsageAsync(Guid id, CancellationToken ct)
        => await _db.CouponUsages.AsNoTracking().Where(u => u.CouponId == id).OrderByDescending(u => u.UsedAt)
            .Select(u => new CouponUsageDto(u.Id, u.CouponId, u.CustomerId, u.OrderId, u.DiscountAmount, u.UsedAt))
            .ToListAsync(ct);

    // ---------- Two-order settings ----------

    public async Task<TwoOrderSettingsDto> GetTwoOrderSettingsAsync(CancellationToken ct)
    {
        var s = await _db.TwoOrderCouponSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null
            ? new TwoOrderSettingsDto(false, 10m, 30, null, false)
            : new TwoOrderSettingsDto(s.IsEnabled, s.DiscountPercentage, s.ValidityDays, s.MinimumOrderSubtotal, s.SendWhatsAppMessage);
    }

    public async Task<TwoOrderSettingsDto> UpdateTwoOrderSettingsAsync(TwoOrderSettingsDto req, CancellationToken ct)
    {
        if (req.DiscountPercentage is < 0 or > 100)
            throw AppException.Validation("Two-order coupon discount percentage must be between 0 and 100.");
        if (req.ValidityDays < 1)
            throw AppException.Validation("Two-order coupon validity must be at least one day.");
        if (req.MinimumOrderSubtotal is < 0)
            throw AppException.Validation("Minimum subtotal cannot be negative.");
        var s = await _db.TwoOrderCouponSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new TwoOrderCouponSettings { Id = Guid.NewGuid() };
            _db.TwoOrderCouponSettings.Add(s);
        }
        s.IsEnabled = req.IsEnabled;
        s.DiscountPercentage = req.DiscountPercentage;
        s.ValidityDays = req.ValidityDays;
        s.MinimumOrderSubtotal = req.MinimumOrderSubtotal;
        s.SendWhatsAppMessage = req.SendWhatsAppMessage;
        s.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return req;
    }

    private static AdminCouponDto Map(Coupon c, int timesUsed)
        => new(c.Id, c.Code, c.Type, c.Value, c.StartAt, c.EndAt, c.TotalUsageLimit, c.PerCustomerUsageLimit,
            c.MinimumOrderSubtotal, c.IsActive, c.IsCustomerSpecific, c.CustomerId, c.Source, timesUsed);

    private static void ValidateUpsert(CouponUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw AppException.Validation("Coupon code is required.");
        if (req.Value <= 0)
            throw AppException.Validation("Coupon value must be greater than zero.");
        if (req.Type == CouponType.Percentage && req.Value > 100)
            throw AppException.Validation("Percentage coupon value must be between 0 and 100.");
        if (req.TotalUsageLimit is < 1)
            throw AppException.Validation("Total usage limit must be at least one.");
        if (req.PerCustomerUsageLimit is < 1)
            throw AppException.Validation("Per-customer usage limit must be at least one.");
        if (req.MinimumOrderSubtotal is < 0)
            throw AppException.Validation("Minimum subtotal cannot be negative.");
        if (req.StartAt is not null && req.EndAt is not null && req.StartAt > req.EndAt)
            throw AppException.Validation("Coupon start date must be before end date.");
        if (req.IsCustomerSpecific && req.CustomerId is null)
            throw AppException.Validation("Customer-specific coupons require a customer.");
    }
}
