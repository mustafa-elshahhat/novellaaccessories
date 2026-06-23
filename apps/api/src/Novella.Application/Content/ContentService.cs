using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Catalog;
using Novella.Application.Common;
using Novella.Domain.Entities;

namespace Novella.Application.Content;

/// <summary>Hero slides, static pages, and the composed storefront home payload.</summary>
public sealed class ContentService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly CatalogPublicService _catalog;

    public ContentService(IAppDbContext db, IClock clock, CatalogPublicService catalog)
    {
        _db = db;
        _clock = clock;
        _catalog = catalog;
    }

    // ---------- Hero ----------

    public async Task<IReadOnlyList<HeroDto>> GetHeroesAsync(bool activeOnly, CancellationToken ct)
        => await _db.HeroSections.AsNoTracking().Where(h => !activeOnly || h.IsActive).OrderBy(h => h.SortOrder)
            .Select(h => MapHero(h)).ToListAsync(ct);

    public async Task<HeroDto> CreateHeroAsync(HeroUpsertRequest req, CancellationToken ct)
    {
        var h = new HeroSection
        {
            Id = Guid.NewGuid(),
            ImageUrl = req.ImageUrl, ImagePublicId = req.ImagePublicId,
            TitleAr = req.TitleAr, TitleEn = req.TitleEn,
            SubtitleAr = req.SubtitleAr, SubtitleEn = req.SubtitleEn,
            CtaTextAr = req.CtaTextAr, CtaTextEn = req.CtaTextEn, CtaLink = req.CtaLink,
            LinkedProductId = req.LinkedProductId, IsActive = req.IsActive, SortOrder = req.SortOrder,
            CreatedAt = _clock.UtcNow
        };
        _db.HeroSections.Add(h);
        await _db.SaveChangesAsync(ct);
        return MapHero(h);
    }

    public async Task<HeroDto> UpdateHeroAsync(Guid id, HeroUpsertRequest req, CancellationToken ct)
    {
        var h = await _db.HeroSections.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Hero not found.");
        h.ImageUrl = req.ImageUrl; h.ImagePublicId = req.ImagePublicId;
        h.TitleAr = req.TitleAr; h.TitleEn = req.TitleEn;
        h.SubtitleAr = req.SubtitleAr; h.SubtitleEn = req.SubtitleEn;
        h.CtaTextAr = req.CtaTextAr; h.CtaTextEn = req.CtaTextEn; h.CtaLink = req.CtaLink;
        h.LinkedProductId = req.LinkedProductId; h.IsActive = req.IsActive; h.SortOrder = req.SortOrder;
        h.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapHero(h);
    }

    public async Task DeleteHeroAsync(Guid id, CancellationToken ct)
    {
        var h = await _db.HeroSections.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Hero not found.");
        _db.HeroSections.Remove(h);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetHeroStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var h = await _db.HeroSections.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Hero not found.");
        h.IsActive = isActive; h.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderHeroesAsync(ReorderRequest req, CancellationToken ct)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var heroes = await _db.HeroSections.Where(h => ids.Contains(h.Id)).ToListAsync(ct);
        foreach (var h in heroes)
            h.SortOrder = req.Items.First(i => i.Id == h.Id).SortOrder;
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Static pages ----------

    public async Task<IReadOnlyList<StaticPageDto>> GetPagesAsync(CancellationToken ct)
        => await _db.StaticPages.AsNoTracking().OrderBy(p => p.Key).Select(p => MapPage(p)).ToListAsync(ct);

    public async Task<StaticPageDto> GetPageByIdAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.StaticPages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Page not found.");
        return MapPage(p);
    }

    public async Task<StaticPageDto> GetPageBySlugAsync(string slug, CancellationToken ct)
    {
        var p = await _db.StaticPages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug || x.Key == slug), ct)
            ?? throw AppException.NotFound("Page not found.");
        return MapPage(p);
    }

    public async Task<StaticPageDto> GetFaqAsync(CancellationToken ct)
    {
        var p = await _db.StaticPages.AsNoTracking().FirstOrDefaultAsync(x => x.Key == "faq", ct)
            ?? throw AppException.NotFound("FAQ page not found.");
        return MapPage(p);
    }

    public async Task<StaticPageDto> UpdatePageAsync(Guid id, StaticPageUpdateRequest req, CancellationToken ct)
    {
        var p = await _db.StaticPages.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Page not found.");
        // The fixed key and generated slugs are immutable application concerns and are never changed here.
        p.TitleAr = req.TitleAr; p.TitleEn = req.TitleEn;
        p.ContentAr = req.ContentAr; p.ContentEn = req.ContentEn;
        p.IsActive = req.IsActive; p.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapPage(p);
    }

    // ---------- Home ----------

    public async Task<HomeDto> GetHomeAsync(CancellationToken ct)
    {
        var heroes = await GetHeroesAsync(activeOnly: true, ct);
        var categories = await _catalog.GetCategoriesAsync(ct);
        var featured = await _catalog.GetFeaturedAsync(ct);
        return new HomeDto(heroes, categories, featured);
    }

    private static HeroDto MapHero(HeroSection h) => new(
        h.Id, h.ImageUrl, h.ImagePublicId, h.TitleAr, h.TitleEn, h.SubtitleAr, h.SubtitleEn,
        h.CtaTextAr, h.CtaTextEn, h.CtaLink, h.LinkedProductId, h.IsActive, h.SortOrder);

    private static StaticPageDto MapPage(StaticPage p) => new(
        p.Id, p.Key, p.TitleAr, p.TitleEn, p.SlugAr, p.SlugEn, p.ContentAr, p.ContentEn, p.IsActive);
}
