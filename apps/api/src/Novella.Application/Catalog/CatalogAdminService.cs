using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Catalog;

/// <summary>Admin catalog management: categories, products, variants, images, stock.</summary>
public sealed class CatalogAdminService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CatalogAdminService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // ---------- Categories ----------

    public async Task<IReadOnlyList<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct)
        => await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder)
            .Select(c => new AdminCategoryDto(c.Id, c.NameAr, c.NameEn, c.SlugAr, c.SlugEn, c.ImageUrl, c.ImagePublicId,
                c.SortOrder, c.IsActive, c.Products.Count,
                c.SeoTitleAr, c.SeoTitleEn, c.SeoDescriptionAr, c.SeoDescriptionEn,
                c.AeoSummaryAr, c.AeoSummaryEn, c.GeoContentAr, c.GeoContentEn))
            .ToListAsync(ct);

    public async Task<AdminCategoryDto> GetCategoryAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Categories.AsNoTracking().Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Category not found.");
        return new AdminCategoryDto(c.Id, c.NameAr, c.NameEn, c.SlugAr, c.SlugEn, c.ImageUrl, c.ImagePublicId,
            c.SortOrder, c.IsActive, c.Products.Count,
            c.SeoTitleAr, c.SeoTitleEn, c.SeoDescriptionAr, c.SeoDescriptionEn,
            c.AeoSummaryAr, c.AeoSummaryEn, c.GeoContentAr, c.GeoContentEn);
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(CategoryUpsertRequest req, CancellationToken ct)
    {
        ValidateCategory(req);
        var c = new Category
        {
            Id = Guid.NewGuid(),
            NameAr = req.NameAr,
            NameEn = req.NameEn,
            SlugAr = await UniqueCategorySlug(Slug.Ensure(req.SlugAr, req.NameAr), true, null, ct),
            SlugEn = await UniqueCategorySlug(Slug.Ensure(req.SlugEn, req.NameEn), false, null, ct),
            ImageUrl = req.ImageUrl,
            ImagePublicId = req.ImagePublicId,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            SeoTitleAr = req.SeoTitleAr, SeoTitleEn = req.SeoTitleEn,
            SeoDescriptionAr = req.SeoDescriptionAr, SeoDescriptionEn = req.SeoDescriptionEn,
            AeoSummaryAr = req.AeoSummaryAr, AeoSummaryEn = req.AeoSummaryEn,
            GeoContentAr = req.GeoContentAr, GeoContentEn = req.GeoContentEn,
            CreatedAt = _clock.UtcNow
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync(ct);
        return await GetCategoryAsync(c.Id, ct);
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(Guid id, CategoryUpsertRequest req, CancellationToken ct)
    {
        ValidateCategory(req);
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Category not found.");
        c.NameAr = req.NameAr; c.NameEn = req.NameEn;
        c.SlugAr = await UniqueCategorySlug(Slug.Ensure(req.SlugAr, req.NameAr), true, id, ct);
        c.SlugEn = await UniqueCategorySlug(Slug.Ensure(req.SlugEn, req.NameEn), false, id, ct);
        c.ImageUrl = req.ImageUrl; c.ImagePublicId = req.ImagePublicId;
        c.SortOrder = req.SortOrder; c.IsActive = req.IsActive;
        c.SeoTitleAr = req.SeoTitleAr; c.SeoTitleEn = req.SeoTitleEn;
        c.SeoDescriptionAr = req.SeoDescriptionAr; c.SeoDescriptionEn = req.SeoDescriptionEn;
        c.AeoSummaryAr = req.AeoSummaryAr; c.AeoSummaryEn = req.AeoSummaryEn;
        c.GeoContentAr = req.GeoContentAr; c.GeoContentEn = req.GeoContentEn;
        c.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetCategoryAsync(id, ct);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Category not found.");
        if (await _db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw AppException.Conflict("Category has products and cannot be deleted.");
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetCategoryStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Category not found.");
        c.IsActive = isActive; c.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderCategoriesAsync(ReorderRequest req, CancellationToken ct)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var cats = await _db.Categories.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        foreach (var c in cats)
            c.SortOrder = req.Items.First(i => i.Id == c.Id).SortOrder;
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Products ----------

    public async Task<PagedResult<AdminProductDto>> GetProductsAsync(PageQuery query, string? search, Guid? categoryId, bool? isActive, bool? isFeatured, CancellationToken ct)
    {
        var q = _db.Products.AsNoTracking().Include(p => p.Variants).Include(p => p.Images).AsQueryable();
        if (categoryId is { } cat) q = q.Where(p => p.CategoryId == cat);
        if (isActive is { } active) q = q.Where(p => p.IsActive == active);
        if (isFeatured is { } featured) q = q.Where(p => p.IsFeatured == featured);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.NameAr.Contains(s) || p.NameEn.Contains(s) || p.Variants.Any(v => v.Sku.Contains(s)));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResult<AdminProductDto>
        {
            Items = items.Select(MapAdminProduct).ToList(),
            Page = query.Page, PageSize = query.PageSize, TotalCount = total
        };
    }

    public async Task<AdminProductDto> GetProductAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Variants).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Product not found.");
        return MapAdminProduct(p);
    }

    public async Task<AdminProductDto> CreateProductAsync(ProductUpsertRequest req, CancellationToken ct)
    {
        ValidateProduct(req);
        await EnsureCategoryExists(req.CategoryId, ct);
        var p = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = req.CategoryId,
            NameAr = req.NameAr, NameEn = req.NameEn,
            SlugAr = await UniqueProductSlug(Slug.Ensure(req.SlugAr, req.NameAr), true, null, ct),
            SlugEn = await UniqueProductSlug(Slug.Ensure(req.SlugEn, req.NameEn), false, null, ct),
            DescriptionAr = req.DescriptionAr, DescriptionEn = req.DescriptionEn,
            BasePurchasePrice = req.BasePurchasePrice, BaseSellingPrice = req.BaseSellingPrice,
            ProductDiscountPercentage = req.ProductDiscountPercentage,
            ProductDiscountStartAt = req.ProductDiscountStartAt, ProductDiscountEndAt = req.ProductDiscountEndAt,
            IsFeatured = req.IsFeatured, IsActive = req.IsActive,
            SeoTitleAr = req.SeoTitleAr, SeoTitleEn = req.SeoTitleEn,
            SeoDescriptionAr = req.SeoDescriptionAr, SeoDescriptionEn = req.SeoDescriptionEn,
            AeoSummaryAr = req.AeoSummaryAr, AeoSummaryEn = req.AeoSummaryEn,
            GeoContentAr = req.GeoContentAr, GeoContentEn = req.GeoContentEn,
            CreatedAt = _clock.UtcNow
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return await GetProductAsync(p.Id, ct);
    }

    public async Task<AdminProductDto> UpdateProductAsync(Guid id, ProductUpsertRequest req, CancellationToken ct)
    {
        ValidateProduct(req);
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Product not found.");
        await EnsureCategoryExists(req.CategoryId, ct);
        p.CategoryId = req.CategoryId;
        p.NameAr = req.NameAr; p.NameEn = req.NameEn;
        p.SlugAr = await UniqueProductSlug(Slug.Ensure(req.SlugAr, req.NameAr), true, id, ct);
        p.SlugEn = await UniqueProductSlug(Slug.Ensure(req.SlugEn, req.NameEn), false, id, ct);
        p.DescriptionAr = req.DescriptionAr; p.DescriptionEn = req.DescriptionEn;
        p.BasePurchasePrice = req.BasePurchasePrice; p.BaseSellingPrice = req.BaseSellingPrice;
        p.ProductDiscountPercentage = req.ProductDiscountPercentage;
        p.ProductDiscountStartAt = req.ProductDiscountStartAt; p.ProductDiscountEndAt = req.ProductDiscountEndAt;
        p.IsFeatured = req.IsFeatured; p.IsActive = req.IsActive;
        p.SeoTitleAr = req.SeoTitleAr; p.SeoTitleEn = req.SeoTitleEn;
        p.SeoDescriptionAr = req.SeoDescriptionAr; p.SeoDescriptionEn = req.SeoDescriptionEn;
        p.AeoSummaryAr = req.AeoSummaryAr; p.AeoSummaryEn = req.AeoSummaryEn;
        p.GeoContentAr = req.GeoContentAr; p.GeoContentEn = req.GeoContentEn;
        p.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetProductAsync(id, ct);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Product not found.");
        if (await _db.OrderItems.AnyAsync(oi => oi.ProductId == id, ct))
        {
            // Preserve order history: soft-deactivate instead of hard delete.
            p.IsActive = false; p.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }
        _db.Products.Remove(p);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetProductStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("Product not found.");
        p.IsActive = isActive; p.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Images ----------

    public async Task<PublicProductImageDto> AddProductImageAsync(Guid productId, AddImageRequest req, CancellationToken ct)
    {
        ValidateImage(req);
        await EnsureProductExists(productId, ct);
        var maxSort = await _db.ProductImages.Where(i => i.ProductId == productId).Select(i => (int?)i.SortOrder).MaxAsync(ct) ?? -1;
        if (req.IsPrimary)
        {
            var currentPrimaries = await _db.ProductImages.Where(i => i.ProductId == productId && i.IsPrimary).ToListAsync(ct);
            foreach (var existing in currentPrimaries)
                existing.IsPrimary = false;
        }
        var img = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Url = req.Url,
            PublicId = req.PublicId,
            AltAr = req.AltAr, AltEn = req.AltEn,
            SortOrder = maxSort + 1,
            IsPrimary = req.IsPrimary,
            CreatedAt = _clock.UtcNow
        };
        _db.ProductImages.Add(img);
        await _db.SaveChangesAsync(ct);
        return new PublicProductImageDto(img.Id, img.Url, img.AltAr, img.AltEn, img.SortOrder, img.IsPrimary);
    }

    public async Task<string?> RemoveProductImageAsync(Guid productId, Guid imageId, CancellationToken ct)
    {
        var img = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct)
            ?? throw AppException.NotFound("Image not found.");
        var publicId = img.PublicId;
        _db.ProductImages.Remove(img);
        await _db.SaveChangesAsync(ct);
        return publicId;
    }

    public async Task ReorderProductImagesAsync(Guid productId, ReorderRequest req, CancellationToken ct)
    {
        var ids = req.Items.Select(i => i.Id).ToList();
        var imgs = await _db.ProductImages.Where(i => i.ProductId == productId && ids.Contains(i.Id)).ToListAsync(ct);
        foreach (var i in imgs)
            i.SortOrder = req.Items.First(x => x.Id == i.Id).SortOrder;
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Variants ----------

    public async Task<IReadOnlyList<AdminVariantDto>> GetVariantsAsync(Guid productId, CancellationToken ct)
        => await _db.ProductVariants.AsNoTracking().Where(v => v.ProductId == productId)
            .Select(v => MapVariant(v)).ToListAsync(ct);

    public async Task<AdminVariantDto> CreateVariantAsync(Guid productId, VariantUpsertRequest req, CancellationToken ct)
    {
        ValidateVariant(req);
        await EnsureProductExists(productId, ct);
        if (await _db.ProductVariants.AnyAsync(v => v.Sku == req.Sku, ct))
            throw AppException.Conflict($"SKU '{req.Sku}' already exists.");

        var v = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = req.Sku,
            NameAr = req.NameAr, NameEn = req.NameEn, Size = req.Size,
            ColorAr = req.ColorAr, ColorEn = req.ColorEn, MaterialAr = req.MaterialAr, MaterialEn = req.MaterialEn,
            CustomOptionNameAr = req.CustomOptionNameAr, CustomOptionNameEn = req.CustomOptionNameEn,
            CustomOptionValueAr = req.CustomOptionValueAr, CustomOptionValueEn = req.CustomOptionValueEn,
            StockQuantity = req.StockQuantity,
            PurchasePriceOverride = req.PurchasePriceOverride, SellingPriceOverride = req.SellingPriceOverride,
            IsActive = req.IsActive,
            CreatedAt = _clock.UtcNow
        };
        _db.ProductVariants.Add(v);
        if (req.StockQuantity != 0)
            _db.InventoryMovements.Add(NewMovement(v.Id, null, MovementType.ManualAdjustment, req.StockQuantity, "Initial stock"));
        await _db.SaveChangesAsync(ct);
        return MapVariant(v);
    }

    public async Task<AdminVariantDto> UpdateVariantAsync(Guid variantId, VariantUpsertRequest req, CancellationToken ct)
    {
        ValidateVariant(req);
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.Id == variantId, ct) ?? throw AppException.NotFound("Variant not found.");
        if (v.Sku != req.Sku && await _db.ProductVariants.AnyAsync(x => x.Sku == req.Sku, ct))
            throw AppException.Conflict($"SKU '{req.Sku}' already exists.");

        var delta = req.StockQuantity - v.StockQuantity;
        v.Sku = req.Sku;
        v.NameAr = req.NameAr; v.NameEn = req.NameEn; v.Size = req.Size;
        v.ColorAr = req.ColorAr; v.ColorEn = req.ColorEn; v.MaterialAr = req.MaterialAr; v.MaterialEn = req.MaterialEn;
        v.CustomOptionNameAr = req.CustomOptionNameAr; v.CustomOptionNameEn = req.CustomOptionNameEn;
        v.CustomOptionValueAr = req.CustomOptionValueAr; v.CustomOptionValueEn = req.CustomOptionValueEn;
        v.StockQuantity = req.StockQuantity;
        v.PurchasePriceOverride = req.PurchasePriceOverride; v.SellingPriceOverride = req.SellingPriceOverride;
        v.IsActive = req.IsActive; v.UpdatedAt = _clock.UtcNow;
        if (delta != 0)
            _db.InventoryMovements.Add(NewMovement(v.Id, null, MovementType.ManualAdjustment, delta, "Variant update"));
        await _db.SaveChangesAsync(ct);
        return MapVariant(v);
    }

    public async Task DeleteVariantAsync(Guid variantId, CancellationToken ct)
    {
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.Id == variantId, ct) ?? throw AppException.NotFound("Variant not found.");
        if (await _db.OrderItems.AnyAsync(oi => oi.ProductVariantId == variantId, ct))
        {
            v.IsActive = false; v.UpdatedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }
        _db.ProductVariants.Remove(v);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AdminVariantDto> AdjustStockAsync(Guid variantId, StockAdjustRequest req, Guid? adminId, CancellationToken ct)
    {
        if (req.NewStockQuantity < 0)
            throw AppException.Validation("Stock quantity cannot be negative.");
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.Id == variantId, ct) ?? throw AppException.NotFound("Variant not found.");
        var delta = req.NewStockQuantity - v.StockQuantity;
        v.StockQuantity = req.NewStockQuantity; v.UpdatedAt = _clock.UtcNow;
        if (delta != 0)
        {
            var mv = NewMovement(v.Id, null, MovementType.ManualAdjustment, delta, req.Reason ?? "Stock adjustment");
            mv.CreatedByAdminId = adminId;
            _db.InventoryMovements.Add(mv);
        }
        await _db.SaveChangesAsync(ct);
        return MapVariant(v);
    }

    public async Task SetVariantStatusAsync(Guid variantId, bool isActive, CancellationToken ct)
    {
        var v = await _db.ProductVariants.FirstOrDefaultAsync(x => x.Id == variantId, ct) ?? throw AppException.NotFound("Variant not found.");
        v.IsActive = isActive; v.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetInventoryMovementsAsync(Guid variantId, CancellationToken ct)
        => await _db.InventoryMovements.AsNoTracking()
            .Where(m => m.ProductVariantId == variantId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .Select(m => new InventoryMovementDto(m.Id, m.ProductVariantId, m.OrderId, m.MovementType.ToString(), m.Quantity, m.Reason, m.CreatedAt, m.CreatedByAdminId))
            .ToListAsync(ct);

    // ---------- helpers ----------

    private InventoryMovement NewMovement(Guid variantId, Guid? orderId, MovementType type, int qty, string? reason)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variantId,
            OrderId = orderId,
            MovementType = type,
            Quantity = qty,
            Reason = reason,
            CreatedAt = _clock.UtcNow
        };

    private static void ValidateCategory(CategoryUpsertRequest req)
    {
        Require(req.NameAr, "Arabic category name is required.");
        Require(req.NameEn, "English category name is required.");
        if (req.SortOrder < 0) throw AppException.Validation("Sort order cannot be negative.");
    }

    private static void ValidateProduct(ProductUpsertRequest req)
    {
        Require(req.NameAr, "Arabic product name is required.");
        Require(req.NameEn, "English product name is required.");
        if (req.BasePurchasePrice < 0) throw AppException.Validation("Purchase price cannot be negative.");
        if (req.BaseSellingPrice < 0) throw AppException.Validation("Selling price cannot be negative.");
        if (req.ProductDiscountPercentage is < 0 or > 100)
            throw AppException.Validation("Product discount percentage must be between 0 and 100.");
        if (req.ProductDiscountStartAt is not null && req.ProductDiscountEndAt is not null && req.ProductDiscountStartAt > req.ProductDiscountEndAt)
            throw AppException.Validation("Product discount start date must be before end date.");
    }

    private static void ValidateVariant(VariantUpsertRequest req)
    {
        Require(req.Sku, "SKU is required.");
        if (req.StockQuantity < 0) throw AppException.Validation("Stock quantity cannot be negative.");
        if (req.PurchasePriceOverride is < 0) throw AppException.Validation("Purchase price override cannot be negative.");
        if (req.SellingPriceOverride is < 0) throw AppException.Validation("Selling price override cannot be negative.");
    }

    private static void ValidateImage(AddImageRequest req)
    {
        Require(req.Url, "Image URL is required.");
        Require(req.PublicId, "Image public ID is required.");
        if (!Uri.TryCreate(req.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw AppException.Validation("Image URL must be an absolute HTTPS URL.");
    }

    private static void Require(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw AppException.Validation(message);
    }

    private static AdminProductDto MapAdminProduct(Product p) => new(
        p.Id, p.CategoryId, p.NameAr, p.NameEn, p.SlugAr, p.SlugEn, p.DescriptionAr, p.DescriptionEn,
        p.BasePurchasePrice, p.BaseSellingPrice, p.ProductDiscountPercentage, p.ProductDiscountStartAt, p.ProductDiscountEndAt,
        p.IsFeatured, p.IsActive,
        p.Variants.Select(MapVariant).ToList(),
        p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
            .Select(i => new PublicProductImageDto(i.Id, i.Url, i.AltAr, i.AltEn, i.SortOrder, i.IsPrimary)).ToList(),
        p.SeoTitleAr, p.SeoTitleEn, p.SeoDescriptionAr, p.SeoDescriptionEn,
        p.AeoSummaryAr, p.AeoSummaryEn, p.GeoContentAr, p.GeoContentEn);

    private static AdminVariantDto MapVariant(ProductVariant v) => new(
        v.Id, v.ProductId, v.Sku, v.NameAr, v.NameEn, v.Size, v.ColorAr, v.ColorEn, v.MaterialAr, v.MaterialEn,
        v.StockQuantity, v.PurchasePriceOverride, v.SellingPriceOverride, v.IsActive);

    private async Task EnsureCategoryExists(Guid id, CancellationToken ct)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == id, ct))
            throw AppException.Validation("Category does not exist.");
    }

    private async Task EnsureProductExists(Guid id, CancellationToken ct)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == id, ct))
            throw AppException.NotFound("Product not found.");
    }

    private async Task<string> UniqueCategorySlug(string baseSlug, bool arabic, Guid? excludeId, CancellationToken ct)
    {
        var slug = baseSlug; var i = 1;
        while (await _db.Categories.AnyAsync(c => (arabic ? c.SlugAr : c.SlugEn) == slug && c.Id != excludeId, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private async Task<string> UniqueProductSlug(string baseSlug, bool arabic, Guid? excludeId, CancellationToken ct)
    {
        var slug = baseSlug; var i = 1;
        while (await _db.Products.AnyAsync(p => (arabic ? p.SlugAr : p.SlugEn) == slug && p.Id != excludeId, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }
}
