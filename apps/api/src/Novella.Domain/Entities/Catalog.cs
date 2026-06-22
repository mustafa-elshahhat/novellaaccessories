using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>Product category with bilingual content and SEO/AEO/GEO metadata.</summary>
public class Category
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public string? ImageAltAr { get; set; }
    public string? ImageAltEn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? SeoDescriptionAr { get; set; }
    public string? SeoDescriptionEn { get; set; }
    public string? AeoSummaryAr { get; set; }
    public string? AeoSummaryEn { get; set; }
    public string? GeoContentAr { get; set; }
    public string? GeoContentEn { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// A sellable product. <see cref="BasePurchasePrice"/> is admin-only cost and must never
/// be exposed through customer-facing APIs.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>Admin-only purchase cost. NEVER exposed to customers.</summary>
    public decimal BasePurchasePrice { get; set; }
    public decimal BaseSellingPrice { get; set; }
    public decimal? ProductDiscountPercentage { get; set; }
    public DateTime? ProductDiscountStartAt { get; set; }
    public DateTime? ProductDiscountEndAt { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? SeoDescriptionAr { get; set; }
    public string? SeoDescriptionEn { get; set; }
    public string? AeoSummaryAr { get; set; }
    public string? AeoSummaryEn { get; set; }
    public string? GeoContentAr { get; set; }
    public string? GeoContentEn { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

/// <summary>Product image stored in Cloudinary (secure URL + public id).</summary>
public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string? AltAr { get; set; }
    public string? AltEn { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// A purchasable variant. <see cref="StockQuantity"/> and <see cref="PurchasePriceOverride"/>
/// are admin-only and must never be exposed to customers (customers see available/unavailable only).
/// </summary>
public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Size { get; set; }
    public string? ColorAr { get; set; }
    public string? ColorEn { get; set; }
    public string? MaterialAr { get; set; }
    public string? MaterialEn { get; set; }
    public string? CustomOptionNameAr { get; set; }
    public string? CustomOptionNameEn { get; set; }
    public string? CustomOptionValueAr { get; set; }
    public string? CustomOptionValueEn { get; set; }

    /// <summary>Admin-only exact stock. NEVER exposed to customers.</summary>
    public int StockQuantity { get; set; }
    /// <summary>Admin-only purchase cost override. NEVER exposed to customers.</summary>
    public decimal? PurchasePriceOverride { get; set; }
    public decimal? SellingPriceOverride { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Audit record of every stock change.</summary>
public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Guid? OrderId { get; set; }
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByAdminId { get; set; }
}
