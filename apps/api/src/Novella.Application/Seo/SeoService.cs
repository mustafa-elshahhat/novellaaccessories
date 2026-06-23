using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Catalog;
using Novella.Application.Common;

namespace Novella.Application.Seo;

public sealed record SitemapEntryDto(string Type, string SlugAr, string SlugEn, DateTime LastModified, bool Indexable);
public sealed record SitemapDataDto(string Domain, IReadOnlyList<SitemapEntryDto> Entries);

public sealed record SeoMetadataDto(
    string EntityType, Guid EntityId, string SlugAr, string SlugEn,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn);

public sealed record ProductSeoDto(SeoMetadataDto Meta, PublicProductDto Product);

/// <summary>SEO/AEO/GEO backend support: sitemap data, per-entity metadata, and admin coverage reads.</summary>
public sealed class SeoService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public SeoService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SitemapDataDto> GetSitemapDataAsync(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive)
            .Select(c => new SitemapEntryDto("category", c.SlugAr, c.SlugEn, c.UpdatedAt ?? c.CreatedAt, true)).ToListAsync(ct);
        var products = await _db.Products.AsNoTracking().Where(p => p.IsActive)
            .Select(p => new SitemapEntryDto("product", p.SlugAr, p.SlugEn, p.UpdatedAt ?? p.CreatedAt, true)).ToListAsync(ct);
        var pages = await _db.StaticPages.AsNoTracking().Where(p => p.IsActive)
            .Select(p => new SitemapEntryDto("page", p.SlugAr, p.SlugEn, p.UpdatedAt ?? DateTime.UtcNow, true)).ToListAsync(ct);

        return new SitemapDataDto(BrandDefaults.SiteDomain, categories.Concat(products).Concat(pages).ToList());
    }

    public async Task<ProductSeoDto> GetProductSeoAsync(string slug, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Images).Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug), ct)
            ?? throw AppException.NotFound("Product not found.");
        var meta = new SeoMetadataDto("product", p.Id, p.SlugAr, p.SlugEn,
            p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
            p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn);
        return new ProductSeoDto(meta, CatalogProjection.MapProduct(p, _clock.UtcNow));
    }

    public async Task<SeoMetadataDto> GetCategorySeoAsync(string slug, CancellationToken ct)
    {
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug), ct)
            ?? throw AppException.NotFound("Category not found.");
        return new SeoMetadataDto("category", c.Id, c.SlugAr, c.SlugEn,
            c.SeoTitleAr, c.SeoTitleEn, c.SeoDescriptionAr, c.SeoDescriptionEn,
            c.AeoSummaryAr, c.AeoSummaryEn, c.GeoContentAr, c.GeoContentEn);
    }

    public async Task<SeoMetadataDto> GetPageSeoAsync(string slug, CancellationToken ct)
    {
        var p = await _db.StaticPages.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug || x.Key == slug), ct)
            ?? throw AppException.NotFound("Page not found.");
        return new SeoMetadataDto("page", p.Id, p.SlugAr, p.SlugEn,
            p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
            p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn);
    }

    public async Task<IReadOnlyList<SeoMetadataDto>> GetAdminContentAsync(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking()
            .Select(c => new SeoMetadataDto("category", c.Id, c.SlugAr, c.SlugEn,
                c.SeoTitleAr, c.SeoTitleEn, c.SeoDescriptionAr, c.SeoDescriptionEn,
                c.AeoSummaryAr, c.AeoSummaryEn, c.GeoContentAr, c.GeoContentEn)).ToListAsync(ct);
        var products = await _db.Products.AsNoTracking()
            .Select(p => new SeoMetadataDto("product", p.Id, p.SlugAr, p.SlugEn,
                p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
                p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn)).ToListAsync(ct);
        var pages = await _db.StaticPages.AsNoTracking()
            .Select(p => new SeoMetadataDto("page", p.Id, p.SlugAr, p.SlugEn,
                p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
                p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn)).ToListAsync(ct);
        return categories.Concat(products).Concat(pages).ToList();
    }

}
