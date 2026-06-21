using Novella.Application.Common;

namespace Novella.Application.Catalog;

// ---------- Public projections (NO stock count, NO purchase cost) ----------

public sealed record PublicCategoryDto(
    Guid Id, string NameAr, string NameEn, string SlugAr, string SlugEn,
    string? ImageUrl, int SortOrder,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn);

public sealed record PublicVariantDto(
    Guid Id, string? NameAr, string? NameEn, string? Size,
    string? ColorAr, string? ColorEn, string? MaterialAr, string? MaterialEn,
    string? CustomOptionNameAr, string? CustomOptionNameEn, string? CustomOptionValueAr, string? CustomOptionValueEn,
    decimal OriginalPrice, decimal FinalPrice, bool IsAvailable);

public sealed record PublicProductImageDto(Guid Id, string Url, string? AltAr, string? AltEn, int SortOrder, bool IsPrimary);

public sealed record PublicProductDto(
    Guid Id, Guid CategoryId, string NameAr, string NameEn, string SlugAr, string SlugEn,
    string? DescriptionAr, string? DescriptionEn,
    decimal OriginalPrice, decimal FinalPrice, bool HasDiscount, decimal? DiscountPercentage,
    bool IsAvailable, bool IsFeatured,
    IReadOnlyList<PublicProductImageDto> Images, IReadOnlyList<PublicVariantDto> Variants,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn);

public sealed record PublicProductListItemDto(
    Guid Id, string NameAr, string NameEn, string SlugAr, string SlugEn,
    decimal OriginalPrice, decimal FinalPrice, bool HasDiscount, decimal? DiscountPercentage,
    bool IsAvailable, bool IsFeatured, string? PrimaryImageUrl);

public sealed class ProductListQuery : PageQuery
{
    public string? Search { get; set; }
    public string? CategorySlug { get; set; }
    public bool? Featured { get; set; }
    public bool? HasDiscount { get; set; }
}

// ---------- Admin DTOs (may include cost/stock — admin only) ----------

public sealed record CategoryUpsertRequest(
    string NameAr, string NameEn, string? SlugAr, string? SlugEn,
    string? ImageUrl, string? ImagePublicId, int SortOrder, bool IsActive,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn);

public sealed record AdminCategoryDto(
    Guid Id, string NameAr, string NameEn, string SlugAr, string SlugEn,
    string? ImageUrl, string? ImagePublicId, int SortOrder, bool IsActive, int ProductCount);

public sealed record ProductUpsertRequest(
    Guid CategoryId, string NameAr, string NameEn, string? SlugAr, string? SlugEn,
    string? DescriptionAr, string? DescriptionEn,
    decimal BasePurchasePrice, decimal BaseSellingPrice,
    decimal? ProductDiscountPercentage, DateTime? ProductDiscountStartAt, DateTime? ProductDiscountEndAt,
    bool IsFeatured, bool IsActive,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn);

public sealed record AdminProductDto(
    Guid Id, Guid CategoryId, string NameAr, string NameEn, string SlugAr, string SlugEn,
    string? DescriptionAr, string? DescriptionEn,
    decimal BasePurchasePrice, decimal BaseSellingPrice,
    decimal? ProductDiscountPercentage, DateTime? ProductDiscountStartAt, DateTime? ProductDiscountEndAt,
    bool IsFeatured, bool IsActive,
    IReadOnlyList<AdminVariantDto> Variants, IReadOnlyList<PublicProductImageDto> Images,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn);

public sealed record VariantUpsertRequest(
    string Sku, string? NameAr, string? NameEn, string? Size,
    string? ColorAr, string? ColorEn, string? MaterialAr, string? MaterialEn,
    string? CustomOptionNameAr, string? CustomOptionNameEn, string? CustomOptionValueAr, string? CustomOptionValueEn,
    int StockQuantity, decimal? PurchasePriceOverride, decimal? SellingPriceOverride, bool IsActive);

public sealed record AdminVariantDto(
    Guid Id, Guid ProductId, string Sku, string? NameAr, string? NameEn, string? Size,
    string? ColorAr, string? ColorEn, string? MaterialAr, string? MaterialEn,
    int StockQuantity, decimal? PurchasePriceOverride, decimal? SellingPriceOverride, bool IsActive);

public sealed record StockAdjustRequest(int NewStockQuantity, string? Reason);
public sealed record StatusRequest(bool IsActive);
public sealed record ReorderRequest(IReadOnlyList<ReorderItem> Items);
public sealed record ReorderItem(Guid Id, int SortOrder);
public sealed record AddImageRequest(string Url, string PublicId, string? AltAr, string? AltEn, bool IsPrimary);
