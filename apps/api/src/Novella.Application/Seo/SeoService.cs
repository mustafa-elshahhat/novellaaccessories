using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;

namespace Novella.Application.Seo;

public sealed record SitemapEntryDto(string Type, string SlugAr, string SlugEn, DateTime LastModified, bool Indexable);
public sealed record SitemapDataDto(string Domain, IReadOnlyList<SitemapEntryDto> Entries);

/// <summary>
/// Backend SEO support limited to sitemap data. Per-entity SEO/AEO/GEO metadata is generated
/// automatically by the storefront from the normal Product, Category, and Page APIs — there is no
/// stored technical metadata and no admin SEO workflow.
/// </summary>
public sealed class SeoService
{
    private readonly IAppDbContext _db;

    public SeoService(IAppDbContext db) => _db = db;

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
}
