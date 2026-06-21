using FluentAssertions;
using Novella.Application.Catalog;
using Novella.Domain.Entities;
using Xunit;

namespace Novella.Tests;

/// <summary>
/// Covers the server-side active-discount filter (ProductListQuery.HasDiscount) used by /offers,
/// and confirms public list projections never expose cost or stock.
/// </summary>
public class CatalogPublicServiceTests
{
    private static (CatalogPublicService svc, TestDatabase db, FakeClock clock) Build()
    {
        var db = new TestDatabase();
        var clock = new FakeClock();
        return (new CatalogPublicService(db.Db, clock), db, clock);
    }

    /// <summary>Overrides a product's discount window directly (the seed helper only does ±1 day).</summary>
    private static void SetDiscountWindow(TestDatabase db, Product product, decimal? pct, DateTime? startAt, DateTime? endAt)
    {
        var p = db.Db.Products.Find(product.Id)!;
        p.ProductDiscountPercentage = pct;
        p.ProductDiscountStartAt = startAt;
        p.ProductDiscountEndAt = endAt;
        db.Db.SaveChanges();
    }

    [Fact]
    public async Task HasDiscount_filter_includes_only_currently_active_discounts()
    {
        var (svc, db, clock) = Build();
        using var _ = db;

        // Active discount (window straddles now via the seed helper).
        var (active, _) = TestSeed.AddProduct(db.Db, clock, discountPct: 20m);
        // No discount at all.
        TestSeed.AddProduct(db.Db, clock);
        // Expired discount (ended yesterday).
        var (expired, _) = TestSeed.AddProduct(db.Db, clock, discountPct: 30m);
        SetDiscountWindow(db, expired, 30m, clock.UtcNow.AddDays(-10), clock.UtcNow.AddDays(-1));
        // Future discount (starts tomorrow).
        var (future, _) = TestSeed.AddProduct(db.Db, clock, discountPct: 15m);
        SetDiscountWindow(db, future, 15m, clock.UtcNow.AddDays(1), clock.UtcNow.AddDays(10));

        var result = await svc.GetProductsAsync(
            new ProductListQuery { HasDiscount = true, Page = 1, PageSize = 20 }, default);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(active.Id);
        result.Items[0].HasDiscount.Should().BeTrue();
    }

    [Fact]
    public async Task Without_HasDiscount_filter_all_active_products_are_returned()
    {
        var (svc, db, clock) = Build();
        using var _ = db;

        TestSeed.AddProduct(db.Db, clock, discountPct: 20m);
        TestSeed.AddProduct(db.Db, clock);

        var result = await svc.GetProductsAsync(
            new ProductListQuery { Page = 1, PageSize = 20 }, default);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task HasDiscount_filter_paginates()
    {
        var (svc, db, clock) = Build();
        using var _ = db;

        for (var i = 0; i < 3; i++)
            TestSeed.AddProduct(db.Db, clock, discountPct: 10m);

        var result = await svc.GetProductsAsync(
            new ProductListQuery { HasDiscount = true, Page = 1, PageSize = 2 }, default);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Public_list_projection_exposes_no_cost_or_stock()
    {
        var (svc, db, clock) = Build();
        using var _ = db;

        TestSeed.AddProduct(db.Db, clock, sellingPrice: 1000m, purchasePrice: 600m, stock: 7, discountPct: 20m);

        var result = await svc.GetProductsAsync(
            new ProductListQuery { HasDiscount = true, Page = 1, PageSize = 20 }, default);

        var item = result.Items.Should().ContainSingle().Subject;
        // Customer-safe DTO surface: selling prices + availability only, never cost or stock.
        var props = item.GetType().GetProperties().Select(p => p.Name);
        props.Should().NotContain(new[]
        {
            "BasePurchasePrice", "PurchaseCostPerUnit", "PurchasePriceOverride",
            "StockQuantity", "LineCost", "GrossProfit", "NetProfit",
        });
        item.IsAvailable.Should().BeTrue();
    }
}
