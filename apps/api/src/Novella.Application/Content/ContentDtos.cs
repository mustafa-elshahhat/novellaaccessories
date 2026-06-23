using Novella.Application.Catalog;

namespace Novella.Application.Content;

public sealed record HeroDto(
    Guid Id, string ImageUrl, string ImagePublicId, string TitleAr, string TitleEn,
    string? SubtitleAr, string? SubtitleEn, string? CtaTextAr, string? CtaTextEn, string? CtaLink,
    Guid? LinkedProductId, bool IsActive, int SortOrder);

public sealed record HeroUpsertRequest(
    string ImageUrl, string ImagePublicId, string TitleAr, string TitleEn,
    string? SubtitleAr, string? SubtitleEn, string? CtaTextAr, string? CtaTextEn, string? CtaLink,
    Guid? LinkedProductId, bool IsActive, int SortOrder);

// Key and slugs are internal application concerns and are not exposed for editing. SEO/AEO/GEO
// metadata is generated automatically by the storefront from the page title and content.
public sealed record StaticPageDto(
    Guid Id, string Key, string TitleAr, string TitleEn, string SlugAr, string SlugEn,
    string ContentAr, string ContentEn, bool IsActive);

public sealed record StaticPageUpdateRequest(
    string TitleAr, string TitleEn, string ContentAr, string ContentEn, bool IsActive);

public sealed record HomeDto(
    IReadOnlyList<HeroDto> Heroes,
    IReadOnlyList<PublicCategoryDto> Categories,
    IReadOnlyList<PublicProductListItemDto> FeaturedProducts);
