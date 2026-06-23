using System;
using System.Linq;
using FluentAssertions;
using Novella.Application.Catalog;
using Novella.Application.Content;
using Novella.Domain.Entities;
using Xunit;

namespace Novella.Tests;

/// <summary>
/// Covers system-owned slug behavior and the removal of manual SEO/AEO/GEO management: slugs are
/// generated on creation, resolve collisions, and stay stable on rename (so published URLs never
/// break); static-page key and slugs are immutable; admin upsert contracts expose no slug or
/// technical SEO/AEO/GEO inputs; category descriptions are normal business content.
/// </summary>
public sealed class CatalogAdminCleanupTests
{
    private static CatalogAdminService AdminSvc(TestDatabase db, FakeClock clock) => new(db.Db, clock);

    private static CategoryUpsertRequest CategoryReq(string nameEn, string nameAr = "فئة",
        string? descAr = null, string? descEn = null) =>
        new(nameAr, nameEn, descAr, descEn, null, null, null, null, 0, true);

    private static ProductUpsertRequest ProductReq(Guid categoryId, string nameEn, string nameAr = "منتج") =>
        new(categoryId, nameAr, nameEn, null, null, 600m, 1000m, null, null, null, false, true);

    [Fact]
    public async Task Category_creation_generates_a_slug_from_the_name()
    {
        using var db = new TestDatabase();
        var svc = AdminSvc(db, new FakeClock());

        var created = await svc.CreateCategoryAsync(CategoryReq("Necklaces"), default);

        created.SlugEn.Should().Be("necklaces");
        created.SlugAr.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Category_creation_resolves_slug_collisions()
    {
        using var db = new TestDatabase();
        var svc = AdminSvc(db, new FakeClock());

        var first = await svc.CreateCategoryAsync(CategoryReq("Rings"), default);
        var second = await svc.CreateCategoryAsync(CategoryReq("Rings"), default);

        first.SlugEn.Should().Be("rings");
        second.SlugEn.Should().Be("rings-2");
    }

    [Fact]
    public async Task Category_rename_keeps_the_published_slug_and_stores_descriptions()
    {
        using var db = new TestDatabase();
        var svc = AdminSvc(db, new FakeClock());

        var created = await svc.CreateCategoryAsync(CategoryReq("Bracelets"), default);
        var updated = await svc.UpdateCategoryAsync(created.Id,
            CategoryReq("Fancy Bracelets", "أساور أنيقة", "وصف عربي", "English description"), default);

        updated.SlugEn.Should().Be(created.SlugEn);
        updated.SlugAr.Should().Be(created.SlugAr);
        updated.DescriptionEn.Should().Be("English description");
        updated.DescriptionAr.Should().Be("وصف عربي");
    }

    [Fact]
    public async Task Product_creation_generates_unique_slugs_and_resolves_collisions()
    {
        using var db = new TestDatabase();
        var svc = AdminSvc(db, new FakeClock());
        var cat = await svc.CreateCategoryAsync(CategoryReq("Earrings"), default);

        var p1 = await svc.CreateProductAsync(ProductReq(cat.Id, "Nova Earrings"), default);
        var p2 = await svc.CreateProductAsync(ProductReq(cat.Id, "Nova Earrings"), default);

        p1.SlugEn.Should().Be("nova-earrings");
        p2.SlugEn.Should().Be("nova-earrings-2");
        p1.SlugAr.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Product_rename_does_not_change_the_published_slug()
    {
        using var db = new TestDatabase();
        var svc = AdminSvc(db, new FakeClock());
        var cat = await svc.CreateCategoryAsync(CategoryReq("Earrings"), default);
        var created = await svc.CreateProductAsync(ProductReq(cat.Id, "Luna Ring"), default);

        var updated = await svc.UpdateProductAsync(created.Id, ProductReq(cat.Id, "Totally Different Name"), default);

        updated.SlugEn.Should().Be(created.SlugEn);
        updated.SlugAr.Should().Be(created.SlugAr);
    }

    [Fact]
    public async Task Static_page_update_cannot_change_key_or_slug()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var page = new StaticPage
        {
            Id = Guid.NewGuid(), Key = "about", TitleAr = "من نحن", TitleEn = "About",
            SlugAr = "about", SlugEn = "about", ContentAr = "م", ContentEn = "c", IsActive = true
        };
        db.Db.StaticPages.Add(page);
        db.Db.SaveChanges();

        var content = new ContentService(db.Db, clock, new CatalogPublicService(db.Db, clock));
        var updated = await content.UpdatePageAsync(page.Id,
            new StaticPageUpdateRequest("عنوان جديد", "New Title", "م", "New content", true), default);

        updated.Key.Should().Be("about");
        updated.SlugEn.Should().Be("about");
        updated.SlugAr.Should().Be("about");
        updated.TitleEn.Should().Be("New Title");
    }

    [Fact]
    public void Admin_upsert_contracts_expose_no_slug_or_seo_inputs()
    {
        var banned = new[] { "Slug", "Seo", "Aeo", "Geo" };
        foreach (var t in new[] { typeof(ProductUpsertRequest), typeof(CategoryUpsertRequest), typeof(StaticPageUpdateRequest) })
        {
            var names = t.GetProperties().Select(p => p.Name).ToList();
            names.Should().NotContain(
                n => banned.Any(b => n.Contains(b, StringComparison.OrdinalIgnoreCase)),
                $"{t.Name} must not accept system-owned slug or technical SEO/AEO/GEO inputs");
        }
    }
}
