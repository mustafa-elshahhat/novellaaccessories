using Novella.Domain.Entities;
using Novella.Domain.Services;

namespace Novella.Application.Catalog;

/// <summary>
/// Builds customer-safe projections. CRITICAL INVARIANT: these projections never include
/// purchase cost or exact stock quantity — only available/unavailable and selling prices.
/// </summary>
public static class CatalogProjection
{
    public static decimal VariantOriginalPrice(Product p, ProductVariant v)
        => v.SellingPriceOverride ?? p.BaseSellingPrice;

    public static decimal ApplyDiscount(decimal original, Product p, DateTime nowUtc)
    {
        if (!PricingCalculator.IsProductDiscountActive(p.ProductDiscountPercentage, p.ProductDiscountStartAt, p.ProductDiscountEndAt, nowUtc))
            return original;
        var discount = PricingCalculator.Round(original * p.ProductDiscountPercentage!.Value / 100m);
        var final = original - discount;
        return final < 0 ? 0 : final;
    }

    public static bool VariantAvailable(ProductVariant v) => v.IsActive && v.StockQuantity > 0;

    public static bool ProductAvailable(Product p)
        => p.IsActive && p.Variants.Any(VariantAvailable);

    public static PublicVariantDto MapVariant(Product p, ProductVariant v, DateTime nowUtc)
    {
        var original = VariantOriginalPrice(p, v);
        var final = ApplyDiscount(original, p, nowUtc);
        return new PublicVariantDto(
            v.Id, v.NameAr, v.NameEn, v.Size, v.ColorAr, v.ColorEn, v.MaterialAr, v.MaterialEn,
            v.CustomOptionNameAr, v.CustomOptionNameEn, v.CustomOptionValueAr, v.CustomOptionValueEn,
            original, final, VariantAvailable(v));
    }

    public static PublicProductDto MapProduct(Product p, DateTime nowUtc)
    {
        var original = p.BaseSellingPrice;
        var final = ApplyDiscount(original, p, nowUtc);
        var hasDiscount = final < original;

        var images = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
            .Select(i => new PublicProductImageDto(i.Id, i.Url, i.AltAr, i.AltEn, i.SortOrder, i.IsPrimary)).ToList();

        var variants = p.Variants.Where(v => v.IsActive)
            .Select(v => MapVariant(p, v, nowUtc)).ToList();

        return new PublicProductDto(
            p.Id, p.CategoryId, p.NameAr, p.NameEn, p.SlugAr, p.SlugEn, p.DescriptionAr, p.DescriptionEn,
            original, final, hasDiscount,
            hasDiscount ? p.ProductDiscountPercentage : null,
            ProductAvailable(p), p.IsFeatured, images, variants,
            p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
            p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn);
    }

    public static PublicProductListItemDto MapListItem(Product p, DateTime nowUtc)
    {
        var original = p.BaseSellingPrice;
        var final = ApplyDiscount(original, p, nowUtc);
        var hasDiscount = final < original;
        var primary = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault();
        return new PublicProductListItemDto(
            p.Id, p.NameAr, p.NameEn, p.SlugAr, p.SlugEn,
            original, final, hasDiscount, hasDiscount ? p.ProductDiscountPercentage : null,
            ProductAvailable(p), p.IsFeatured, primary?.Url);
    }
}
