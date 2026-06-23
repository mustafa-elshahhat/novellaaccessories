using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;

namespace Novella.Application.Catalog;

/// <summary>Read-only storefront catalog. Returns customer-safe projections only.</summary>
public sealed class CatalogPublicService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CatalogPublicService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PublicCategoryDto>> GetCategoriesAsync(CancellationToken ct)
        => await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new PublicCategoryDto(c.Id, c.NameAr, c.NameEn, c.SlugAr, c.SlugEn,
                c.DescriptionAr, c.DescriptionEn, c.ImageUrl, c.ImageAltAr, c.ImageAltEn, c.SortOrder))
            .ToListAsync(ct);

    public async Task<PublicCategoryDto> GetCategoryBySlugAsync(string slug, CancellationToken ct)
    {
        var c = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug), ct)
            ?? throw AppException.NotFound("Category not found.");
        return new PublicCategoryDto(c.Id, c.NameAr, c.NameEn, c.SlugAr, c.SlugEn,
            c.DescriptionAr, c.DescriptionEn, c.ImageUrl, c.ImageAltAr, c.ImageAltEn, c.SortOrder);
    }

    public async Task<PagedResult<PublicProductListItemDto>> GetCategoryProductsAsync(string slug, ProductListQuery query, CancellationToken ct)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug), ct)
            ?? throw AppException.NotFound("Category not found.");
        query.CategorySlug = null;
        return await ListProductsAsync(query, ct, category.Id);
    }

    public Task<PagedResult<PublicProductListItemDto>> GetProductsAsync(ProductListQuery query, CancellationToken ct)
        => ListProductsAsync(query, ct, null);

    private async Task<PagedResult<PublicProductListItemDto>> ListProductsAsync(ProductListQuery query, CancellationToken ct, Guid? categoryId)
    {
        var now = _clock.UtcNow;
        var q = _db.Products.AsNoTracking().Include(p => p.Images).Include(p => p.Variants)
            .Where(p => p.IsActive);

        if (categoryId is { } cid)
            q = q.Where(p => p.CategoryId == cid);
        else if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            q = q.Where(p => p.Category!.SlugAr == query.CategorySlug || p.Category!.SlugEn == query.CategorySlug);

        if (query.Featured is true)
            q = q.Where(p => p.IsFeatured);

        // Active product-discount filter (mirrors PricingCalculator.IsProductDiscountActive) for /offers.
        if (query.HasDiscount is true)
            q = q.Where(p => p.ProductDiscountPercentage > 0m
                && (p.ProductDiscountStartAt == null || now >= p.ProductDiscountStartAt)
                && (p.ProductDiscountEndAt == null || now <= p.ProductDiscountEndAt));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => p.NameAr.Contains(s) || p.NameEn.Contains(s)
                || (p.DescriptionAr != null && p.DescriptionAr.Contains(s))
                || (p.DescriptionEn != null && p.DescriptionEn.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return new PagedResult<PublicProductListItemDto>
        {
            Items = items.Select(p => CatalogProjection.MapListItem(p, now)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<PublicProductDto> GetProductBySlugAsync(string slug, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var p = await _db.Products.AsNoTracking().Include(x => x.Images).Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.IsActive && (x.SlugAr == slug || x.SlugEn == slug), ct)
            ?? throw AppException.NotFound("Product not found.");
        return CatalogProjection.MapProduct(p, now);
    }

    public async Task<IReadOnlyList<PublicProductListItemDto>> GetFeaturedAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var items = await _db.Products.AsNoTracking().Include(p => p.Images).Include(p => p.Variants)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt).Take(20).ToListAsync(ct);
        return items.Select(p => CatalogProjection.MapListItem(p, now)).ToList();
    }

    public Task<PagedResult<PublicProductListItemDto>> SearchAsync(string term, ProductListQuery query, CancellationToken ct)
    {
        query.Search = term;
        return ListProductsAsync(query, ct, null);
    }
}
