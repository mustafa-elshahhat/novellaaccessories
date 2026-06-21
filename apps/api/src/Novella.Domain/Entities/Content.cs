namespace Novella.Domain.Entities;

/// <summary>A homepage hero/banner slide.</summary>
public class HeroSection
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePublicId { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? SubtitleAr { get; set; }
    public string? SubtitleEn { get; set; }
    public string? CtaTextAr { get; set; }
    public string? CtaTextEn { get; set; }
    public string? CtaLink { get; set; }
    public Guid? LinkedProductId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>A localized static page (about, contact, privacy, terms, returns, shipping, faq).</summary>
public class StaticPage
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public string ContentEn { get; set; } = string.Empty;

    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? SeoDescriptionAr { get; set; }
    public string? SeoDescriptionEn { get; set; }
    public string? AeoSummaryAr { get; set; }
    public string? AeoSummaryEn { get; set; }
    public string? GeoContentAr { get; set; }
    public string? GeoContentEn { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Singleton global site settings.</summary>
public class SiteSettings
{
    public Guid Id { get; set; }
    public string SiteNameAr { get; set; } = string.Empty;
    public string SiteNameEn { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string? DefaultSeoTitleAr { get; set; }
    public string? DefaultSeoTitleEn { get; set; }
    public string? DefaultSeoDescriptionAr { get; set; }
    public string? DefaultSeoDescriptionEn { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public bool IsFreeShippingEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}
