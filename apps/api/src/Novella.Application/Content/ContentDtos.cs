using Novella.Application.Catalog;

namespace Novella.Application.Content;

public sealed record SiteSettingsDto(
    string SiteNameAr, string SiteNameEn, string Domain,
    string? DefaultSeoTitleAr, string? DefaultSeoTitleEn, string? DefaultSeoDescriptionAr, string? DefaultSeoDescriptionEn,
    decimal? FreeShippingThreshold, bool IsFreeShippingEnabled);

public sealed record HeroDto(
    Guid Id, string ImageUrl, string ImagePublicId, string TitleAr, string TitleEn,
    string? SubtitleAr, string? SubtitleEn, string? CtaTextAr, string? CtaTextEn, string? CtaLink,
    Guid? LinkedProductId, bool IsActive, int SortOrder);

public sealed record HeroUpsertRequest(
    string ImageUrl, string ImagePublicId, string TitleAr, string TitleEn,
    string? SubtitleAr, string? SubtitleEn, string? CtaTextAr, string? CtaTextEn, string? CtaLink,
    Guid? LinkedProductId, bool IsActive, int SortOrder);

public sealed record StaticPageDto(
    Guid Id, string Key, string TitleAr, string TitleEn, string SlugAr, string SlugEn,
    string ContentAr, string ContentEn,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn, bool IsActive);

public sealed record StaticPageUpdateRequest(
    string TitleAr, string TitleEn, string? SlugAr, string? SlugEn, string ContentAr, string ContentEn,
    string? SeoTitleAr, string? SeoTitleEn, string? SeoDescriptionAr, string? SeoDescriptionEn,
    string? AeoSummaryAr, string? AeoSummaryEn, string? GeoContentAr, string? GeoContentEn, bool IsActive);

public sealed record HomeDto(
    SiteSettingsDto SiteSettings,
    IReadOnlyList<HeroDto> Heroes,
    IReadOnlyList<PublicCategoryDto> Categories,
    IReadOnlyList<PublicProductListItemDto> FeaturedProducts);
