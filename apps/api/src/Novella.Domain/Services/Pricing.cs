using Novella.Domain.Enums;

namespace Novella.Domain.Services;

/// <summary>Input for one priced line (variant + quantity) at calculation time.</summary>
public sealed record PricingLineInput(
    Guid ProductId,
    Guid ProductVariantId,
    decimal OriginalUnitSellingPrice,
    decimal? ProductDiscountPercentage,
    DateTime? DiscountStartAt,
    DateTime? DiscountEndAt,
    decimal PurchaseCostPerUnit,
    int Quantity);

/// <summary>A coupon to apply to the product subtotal (never shipping).</summary>
public sealed record CouponInput(CouponType Type, decimal Value);

/// <summary>Computed pricing snapshot for one line.</summary>
public sealed class PricingLineResult
{
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public int Quantity { get; init; }
    public decimal OriginalUnitSellingPrice { get; init; }
    public decimal? ProductDiscountPercentage { get; init; }
    public decimal ProductDiscountAmountPerUnit { get; init; }
    public decimal UnitPriceAfterProductDiscount { get; init; }
    public decimal CouponDiscountAmountPerUnit { get; init; }
    public decimal FinalUnitPrice { get; init; }
    public decimal PurchaseCostPerUnit { get; init; }
    public decimal LineRevenue { get; init; }
    public decimal LineCost { get; init; }
    public decimal LineGrossProfit { get; init; }
}

/// <summary>Computed pricing snapshot for the whole basket.</summary>
public sealed class PricingResult
{
    public IReadOnlyList<PricingLineResult> Lines { get; init; } = Array.Empty<PricingLineResult>();
    public decimal ProductSubtotalBeforeDiscount { get; init; }
    public decimal ProductDiscountTotal { get; init; }
    public decimal SubtotalAfterProductDiscount { get; init; }
    public decimal CouponDiscountTotal { get; init; }
    public decimal ProductSubtotalAfterAllDiscounts { get; init; }
}

/// <summary>
/// Pure pricing engine. Applies the product-level discount first, then a coupon to the
/// resulting product subtotal only. Backend is the single source of truth for all totals;
/// this method is shared by cart reprice, checkout preview, and order creation so that the
/// preview always equals the created order.
/// </summary>
public static class PricingCalculator
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>A product discount is active only when a positive percentage is set and now is within the window.</summary>
    public static bool IsProductDiscountActive(decimal? percentage, DateTime? startAt, DateTime? endAt, DateTime nowUtc)
        => percentage is > 0m
           && (startAt is null || nowUtc >= startAt)
           && (endAt is null || nowUtc <= endAt);

    public static PricingResult Calculate(IReadOnlyList<PricingLineInput> lines, CouponInput? coupon, DateTime nowUtc)
    {
        // Pass 1 — product discount per line.
        var afterProductDiscount = new decimal[lines.Count];
        var discountPerUnit = new decimal[lines.Count];
        var activePct = new decimal?[lines.Count];

        decimal subtotalBeforeDiscount = 0m;
        decimal productDiscountTotal = 0m;
        decimal subtotalAfterProductDiscount = 0m;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var original = line.OriginalUnitSellingPrice;

            decimal perUnitDiscount = 0m;
            decimal? pct = null;
            if (IsProductDiscountActive(line.ProductDiscountPercentage, line.DiscountStartAt, line.DiscountEndAt, nowUtc))
            {
                pct = line.ProductDiscountPercentage;
                perUnitDiscount = Round(original * line.ProductDiscountPercentage.GetValueOrDefault() / 100m);
                if (perUnitDiscount > original) perUnitDiscount = original;
            }

            var unitAfter = original - perUnitDiscount;
            discountPerUnit[i] = perUnitDiscount;
            afterProductDiscount[i] = unitAfter;
            activePct[i] = pct;

            subtotalBeforeDiscount += original * line.Quantity;
            productDiscountTotal += perUnitDiscount * line.Quantity;
            subtotalAfterProductDiscount += unitAfter * line.Quantity;
        }

        // Pass 2 — coupon against the after-product-discount subtotal.
        decimal couponTotalRaw = 0m;
        if (coupon is not null && subtotalAfterProductDiscount > 0m)
        {
            couponTotalRaw = coupon.Type switch
            {
                CouponType.Percentage => Round(subtotalAfterProductDiscount * coupon.Value / 100m),
                CouponType.FixedAmount => Math.Min(Round(coupon.Value), subtotalAfterProductDiscount),
                _ => 0m
            };
            if (couponTotalRaw < 0m) couponTotalRaw = 0m;
            if (couponTotalRaw > subtotalAfterProductDiscount) couponTotalRaw = subtotalAfterProductDiscount;
        }

        // Allocate the coupon across lines proportionally to each line's after-discount value.
        var couponPerUnit = new decimal[lines.Count];
        if (couponTotalRaw > 0m)
        {
            var lineAlloc = new decimal[lines.Count];
            decimal allocated = 0m;
            var largestIdx = 0;
            decimal largestWeight = -1m;

            for (var i = 0; i < lines.Count; i++)
            {
                var weight = afterProductDiscount[i] * lines[i].Quantity;
                var alloc = subtotalAfterProductDiscount > 0m
                    ? Round(couponTotalRaw * weight / subtotalAfterProductDiscount)
                    : 0m;
                lineAlloc[i] = alloc;
                allocated += alloc;
                if (weight > largestWeight) { largestWeight = weight; largestIdx = i; }
            }

            // Absorb rounding remainder on the largest line.
            lineAlloc[largestIdx] += couponTotalRaw - allocated;

            for (var i = 0; i < lines.Count; i++)
            {
                var qty = lines[i].Quantity;
                couponPerUnit[i] = qty > 0 ? Round(lineAlloc[i] / qty) : 0m;
                if (couponPerUnit[i] > afterProductDiscount[i]) couponPerUnit[i] = afterProductDiscount[i];
            }
        }

        // Pass 3 — build line results; derive order totals from lines for internal consistency.
        var results = new List<PricingLineResult>(lines.Count);
        decimal couponDiscountTotal = 0m;
        decimal subtotalAfterAll = 0m;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var finalUnit = afterProductDiscount[i] - couponPerUnit[i];
            if (finalUnit < 0m) finalUnit = 0m;

            var lineRevenue = finalUnit * line.Quantity;
            var lineCost = line.PurchaseCostPerUnit * line.Quantity;

            couponDiscountTotal += couponPerUnit[i] * line.Quantity;
            subtotalAfterAll += lineRevenue;

            results.Add(new PricingLineResult
            {
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                OriginalUnitSellingPrice = line.OriginalUnitSellingPrice,
                ProductDiscountPercentage = activePct[i],
                ProductDiscountAmountPerUnit = discountPerUnit[i],
                UnitPriceAfterProductDiscount = afterProductDiscount[i],
                CouponDiscountAmountPerUnit = couponPerUnit[i],
                FinalUnitPrice = finalUnit,
                PurchaseCostPerUnit = line.PurchaseCostPerUnit,
                LineRevenue = lineRevenue,
                LineCost = lineCost,
                LineGrossProfit = lineRevenue - lineCost
            });
        }

        return new PricingResult
        {
            Lines = results,
            ProductSubtotalBeforeDiscount = subtotalBeforeDiscount,
            ProductDiscountTotal = productDiscountTotal,
            SubtotalAfterProductDiscount = subtotalAfterProductDiscount,
            CouponDiscountTotal = couponDiscountTotal,
            ProductSubtotalAfterAllDiscounts = subtotalAfterAll
        };
    }
}
