using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;

namespace Novella.Application.Shipping;

// Public projection NEVER includes ActualShippingCost.
public sealed record PublicGovernorateDto(Guid Id, string NameAr, string NameEn, decimal ShippingFee, int SortOrder);

// Admin projection includes the actual cost (admin only).
public sealed record AdminGovernorateDto(
    Guid Id, string NameAr, string NameEn, decimal CustomerPaidShippingFee, decimal ActualShippingCost,
    bool IsActive, int SortOrder);

public sealed record GovernorateUpsertRequest(
    string NameAr, string NameEn, decimal CustomerPaidShippingFee, decimal ActualShippingCost, bool IsActive, int SortOrder);

public sealed record ShippingSettingsDto(decimal? FreeShippingThreshold, bool IsFreeShippingEnabled);

/// <summary>
/// Governorate shipping management. The actual shipping cost is admin-only and never exposed to
/// customers. Order snapshots store paid fee, actual cost, and margin (paid - actual).
/// </summary>
public sealed class ShippingService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ShippingService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PublicGovernorateDto>> GetPublicAsync(CancellationToken ct)
        => await _db.ShippingGovernorates.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.SortOrder)
            .Select(g => new PublicGovernorateDto(g.Id, g.NameAr, g.NameEn, g.CustomerPaidShippingFee, g.SortOrder))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AdminGovernorateDto>> GetAdminAsync(CancellationToken ct)
        => await _db.ShippingGovernorates.AsNoTracking().OrderBy(g => g.SortOrder)
            .Select(g => new AdminGovernorateDto(g.Id, g.NameAr, g.NameEn, g.CustomerPaidShippingFee, g.ActualShippingCost, g.IsActive, g.SortOrder))
            .ToListAsync(ct);

    public async Task<AdminGovernorateDto> CreateAsync(GovernorateUpsertRequest req, CancellationToken ct)
    {
        Validate(req);
        var g = new ShippingGovernorate
        {
            Id = Guid.NewGuid(),
            NameAr = req.NameAr, NameEn = req.NameEn,
            CustomerPaidShippingFee = req.CustomerPaidShippingFee,
            ActualShippingCost = req.ActualShippingCost,
            IsActive = req.IsActive, SortOrder = req.SortOrder,
            CreatedAt = _clock.UtcNow
        };
        _db.ShippingGovernorates.Add(g);
        await _db.SaveChangesAsync(ct);
        return Map(g);
    }

    public async Task<AdminGovernorateDto> UpdateAsync(Guid id, GovernorateUpsertRequest req, CancellationToken ct)
    {
        Validate(req);
        var g = await _db.ShippingGovernorates.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Governorate not found.");
        g.NameAr = req.NameAr; g.NameEn = req.NameEn;
        g.CustomerPaidShippingFee = req.CustomerPaidShippingFee;
        g.ActualShippingCost = req.ActualShippingCost;
        g.IsActive = req.IsActive; g.SortOrder = req.SortOrder;
        g.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(g);
    }

    public async Task SetStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var g = await _db.ShippingGovernorates.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Governorate not found.");
        g.IsActive = isActive; g.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ShippingSettingsDto> GetSettingsAsync(CancellationToken ct)
    {
        var s = await _db.ShippingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null ? new ShippingSettingsDto(null, false) : new ShippingSettingsDto(s.FreeShippingThreshold, s.IsFreeShippingEnabled);
    }

    public async Task<ShippingSettingsDto> UpdateSettingsAsync(ShippingSettingsDto req, CancellationToken ct)
    {
        if (req.FreeShippingThreshold is < 0)
            throw AppException.Validation("Free-shipping threshold cannot be negative.");
        var s = await _db.ShippingSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new ShippingSettings { Id = Guid.NewGuid() };
            _db.ShippingSettings.Add(s);
        }
        s.FreeShippingThreshold = req.FreeShippingThreshold;
        s.IsFreeShippingEnabled = req.IsFreeShippingEnabled;
        s.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return req;
    }

    public async Task<decimal> ApplyFreeShippingAsync(decimal productSubtotalAfterDiscounts, decimal governorateFee, CancellationToken ct)
    {
        var settings = await _db.ShippingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is { IsFreeShippingEnabled: true, FreeShippingThreshold: { } threshold }
            && productSubtotalAfterDiscounts >= threshold)
            return 0m;
        return governorateFee;
    }

    /// <summary>Resolves an active governorate for checkout; throws SHIPPING_GOVERNORATE_INACTIVE otherwise.</summary>
    public async Task<ShippingGovernorate> ResolveActiveAsync(Guid governorateId, CancellationToken ct)
    {
        var g = await _db.ShippingGovernorates.FirstOrDefaultAsync(x => x.Id == governorateId, ct)
            ?? throw AppException.NotFound("Governorate not found.");
        if (!g.IsActive)
            throw new AppException(ErrorCodes.ShippingGovernorateInactive, "Selected governorate is not available for shipping.", 409);
        return g;
    }

    private static AdminGovernorateDto Map(ShippingGovernorate g)
        => new(g.Id, g.NameAr, g.NameEn, g.CustomerPaidShippingFee, g.ActualShippingCost, g.IsActive, g.SortOrder);

    private static void Validate(GovernorateUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NameAr) || string.IsNullOrWhiteSpace(req.NameEn))
            throw AppException.Validation("Arabic and English governorate names are required.");
        if (req.CustomerPaidShippingFee < 0 || req.ActualShippingCost < 0)
            throw AppException.Validation("Shipping fees cannot be negative.");
        if (req.SortOrder < 0)
            throw AppException.Validation("Sort order cannot be negative.");
    }
}
