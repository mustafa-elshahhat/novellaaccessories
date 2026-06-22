using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Services;

namespace Novella.Application.Checkout;

public sealed record LineRequest(Guid ProductVariantId, int Quantity);

/// <summary>A cart/order line resolved against the catalog with availability + stock checks.</summary>
public sealed class ResolvedLine
{
    public required Product Product { get; init; }
    public required ProductVariant Variant { get; init; }
    public int RequestedQuantity { get; init; }
    public int EffectiveQuantity { get; init; }
    public bool Available { get; init; }
    public bool QuantityAdjusted => EffectiveQuantity != RequestedQuantity;
}

/// <summary>
/// Loads catalog rows for a set of lines, enforces availability/stock rules, and builds the
/// pure pricing inputs. Shared by cart reprice, checkout preview, and order creation so all
/// three produce identical numbers (the backend is the single pricing authority).
/// </summary>
public sealed class PricingAssembler
{
    private readonly IAppDbContext _db;

    public PricingAssembler(IAppDbContext db) => _db = db;

    /// <summary>
    /// Resolves lines. When <paramref name="strict"/> is true (checkout/order), unavailable
    /// items or quantities above stock throw; otherwise quantities are clamped and flagged.
    /// </summary>
    public async Task<List<ResolvedLine>> ResolveAsync(IReadOnlyList<LineRequest> lines, bool strict, CancellationToken ct)
    {
        var variantIds = lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variants = await _db.ProductVariants.Include(v => v.Product).ThenInclude(p => p!.Images)
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var resolved = new List<ResolvedLine>(lines.Count);
        foreach (var line in lines)
        {
            if (!variants.TryGetValue(line.ProductVariantId, out var variant) || variant.Product is null)
                throw AppException.NotFound("Product variant not found.");

            var product = variant.Product;
            var purchasable = product.IsActive && variant.IsActive;
            var available = purchasable && variant.StockQuantity > 0;

            if (strict)
            {
                if (!purchasable)
                    throw new AppException(ErrorCodes.ProductUnavailable,
                        $"Product '{product.NameEn}' is unavailable.", 409,
                        new Dictionary<string, object?> { ["variantId"] = variant.Id });
                if (variant.StockQuantity < line.Quantity)
                    throw new AppException(ErrorCodes.VariantOutOfStock,
                        $"Insufficient stock for SKU '{variant.Sku}'.", 409,
                        new Dictionary<string, object?> { ["variantId"] = variant.Id });
            }

            var effective = purchasable ? Math.Min(line.Quantity, variant.StockQuantity) : 0;

            resolved.Add(new ResolvedLine
            {
                Product = product,
                Variant = variant,
                RequestedQuantity = line.Quantity,
                EffectiveQuantity = effective,
                Available = available
            });
        }
        return resolved;
    }

    public static List<PricingLineInput> ToPricingInputs(IEnumerable<ResolvedLine> lines, bool useEffectiveQuantity = true)
        => lines.Where(l => (useEffectiveQuantity ? l.EffectiveQuantity : l.RequestedQuantity) > 0)
            .Select(l => new PricingLineInput(
                l.Product.Id,
                l.Variant.Id,
                CatalogPrice(l.Product, l.Variant),
                l.Product.ProductDiscountPercentage,
                l.Product.ProductDiscountStartAt,
                l.Product.ProductDiscountEndAt,
                PurchaseCost(l.Product, l.Variant),
                useEffectiveQuantity ? l.EffectiveQuantity : l.RequestedQuantity))
            .ToList();

    public static decimal CatalogPrice(Product p, ProductVariant v) => v.SellingPriceOverride ?? p.BaseSellingPrice;
    public static decimal PurchaseCost(Product p, ProductVariant v) => v.PurchasePriceOverride ?? p.BasePurchasePrice;
}
