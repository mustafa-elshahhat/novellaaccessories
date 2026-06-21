using FluentAssertions;
using Novella.Domain.Enums;
using Novella.Domain.Services;
using Xunit;

namespace Novella.Tests;

public class PricingCalculatorTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static PricingLineInput Line(decimal price, int qty, decimal cost = 0m, decimal? discountPct = null,
        DateTime? start = null, DateTime? end = null)
        => new(Guid.NewGuid(), Guid.NewGuid(), price, discountPct, start, end, cost, qty);

    [Fact]
    public void ProductDiscount_active_window_is_applied()
    {
        var line = Line(1000m, 1, discountPct: 20m, start: Now.AddDays(-1), end: Now.AddDays(1));
        var result = PricingCalculator.Calculate(new[] { line }, null, Now);

        result.ProductDiscountTotal.Should().Be(200m);
        result.SubtotalAfterProductDiscount.Should().Be(800m);
        result.Lines[0].UnitPriceAfterProductDiscount.Should().Be(800m);
    }

    [Fact]
    public void ProductDiscount_outside_window_is_ignored()
    {
        var expired = Line(1000m, 1, discountPct: 20m, start: Now.AddDays(-10), end: Now.AddDays(-1));
        var result = PricingCalculator.Calculate(new[] { expired }, null, Now);

        result.ProductDiscountTotal.Should().Be(0m);
        result.SubtotalAfterProductDiscount.Should().Be(1000m);
    }

    [Fact]
    public void Stacking_applies_product_discount_then_coupon()
    {
        // 1000 -> 20% product discount -> 800 -> 10% coupon -> 720 (matches business rules example).
        var line = Line(1000m, 1, discountPct: 20m, start: Now.AddDays(-1), end: Now.AddDays(1));
        var result = PricingCalculator.Calculate(new[] { line }, new CouponInput(CouponType.Percentage, 10m), Now);

        result.ProductDiscountTotal.Should().Be(200m);
        result.CouponDiscountTotal.Should().Be(80m);
        result.ProductSubtotalAfterAllDiscounts.Should().Be(720m);
        result.Lines[0].FinalUnitPrice.Should().Be(720m);
    }

    [Fact]
    public void Coupon_fixed_amount_is_capped_to_subtotal()
    {
        var line = Line(100m, 1);
        var result = PricingCalculator.Calculate(new[] { line }, new CouponInput(CouponType.FixedAmount, 250m), Now);
        result.CouponDiscountTotal.Should().Be(100m);
        result.ProductSubtotalAfterAllDiscounts.Should().Be(0m);
    }

    [Fact]
    public void Coupon_applies_to_product_subtotal_only_not_shipping()
    {
        // The calculator has no concept of shipping — proving coupons never touch shipping.
        var line = Line(500m, 2);
        var result = PricingCalculator.Calculate(new[] { line }, new CouponInput(CouponType.Percentage, 10m), Now);
        result.SubtotalAfterProductDiscount.Should().Be(1000m);
        result.CouponDiscountTotal.Should().Be(100m);
        result.ProductSubtotalAfterAllDiscounts.Should().Be(900m);
    }

    [Fact]
    public void LineCost_and_profit_use_purchase_cost()
    {
        var line = Line(1000m, 2, cost: 600m);
        var result = PricingCalculator.Calculate(new[] { line }, null, Now);
        var l = result.Lines[0];
        l.LineRevenue.Should().Be(2000m);
        l.LineCost.Should().Be(1200m);
        l.LineGrossProfit.Should().Be(800m);
    }

    [Fact]
    public void Coupon_is_allocated_across_multiple_lines_consistently()
    {
        var a = Line(300m, 1, cost: 100m);
        var b = Line(700m, 1, cost: 200m);
        var result = PricingCalculator.Calculate(new[] { a, b }, new CouponInput(CouponType.Percentage, 10m), Now);

        // Totals are derived from per-line snapshots; sum of line revenues equals subtotal after all discounts.
        result.Lines.Sum(l => l.LineRevenue).Should().Be(result.ProductSubtotalAfterAllDiscounts);
        result.CouponDiscountTotal.Should().Be(100m);
    }
}
