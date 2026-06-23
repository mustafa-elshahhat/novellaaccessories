using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Infrastructure.Configuration;
using Novella.Infrastructure.Persistence;
using Xunit;

namespace Novella.Tests;

/// <summary>Covers the development seed: placeholder upgrade, idempotency, dev-catalog gating, and
/// Cloudinary media persistence (with a fake storage provider) including the no-duplicate-upload and
/// graceful-when-unconfigured behaviors.</summary>
public sealed class DevelopmentSeedTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly FakeClock _clock = new();

    public DevelopmentSeedTests() => EnsureSeedAssets();

    public void Dispose() => _db.Dispose();

    // --- placeholder upgrade ---

    [Fact]
    public async Task Upgrades_static_page_that_still_matches_old_placeholder()
    {
        _db.Db.StaticPages.Add(new StaticPage
        {
            Id = Guid.NewGuid(), Key = "contact", TitleAr = "اتصل بنا", TitleEn = "Contact Us",
            SlugAr = "contact", SlugEn = "contact",
            ContentEn = "Contact Us page content (placeholder).",
            ContentAr = "محتوى صفحة اتصل بنا (نص مؤقت).",
            IsActive = true
        });
        await _db.Db.SaveChangesAsync();

        await CreateSeeder(devCatalog: false, cloudinaryConfigured: false).SeedAsync();

        await using var verify = _db.NewContext();
        var page = await verify.StaticPages.SingleAsync(p => p.Key == "contact");
        page.ContentEn.Should().NotContain("(placeholder)");
        page.ContentEn.Should().Contain("WhatsApp");
    }

    [Fact]
    public async Task Preserves_admin_edited_static_page_content()
    {
        const string adminContent = "Our own about page, edited by the admin.";
        _db.Db.StaticPages.Add(new StaticPage
        {
            Id = Guid.NewGuid(), Key = "about", TitleAr = "من نحن", TitleEn = "About Us",
            SlugAr = "about", SlugEn = "about",
            ContentEn = adminContent, ContentAr = "محتوى محرر",
            IsActive = true
        });
        await _db.Db.SaveChangesAsync();

        await CreateSeeder(devCatalog: false, cloudinaryConfigured: false).SeedAsync();

        await using var verify = _db.NewContext();
        var page = await verify.StaticPages.SingleAsync(p => p.Key == "about");
        page.ContentEn.Should().Be(adminContent);
    }

    // --- idempotency + gating ---

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_pages_categories_products_or_heroes()
    {
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: true).SeedAsync();
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: true).SeedAsync();

        (await _db.Db.StaticPages.CountAsync()).Should().Be(7);
        (await _db.Db.Categories.CountAsync()).Should().Be(4);
        (await _db.Db.Products.CountAsync()).Should().Be(8);
        (await _db.Db.HeroSections.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Development_catalog_is_gated_by_the_flag()
    {
        await CreateSeeder(devCatalog: false, cloudinaryConfigured: false).SeedAsync();
        (await _db.Db.Products.CountAsync()).Should().Be(0);
        (await _db.Db.Categories.CountAsync()).Should().Be(4); // core categories still seed
    }

    [Fact]
    public async Task Development_catalog_seeds_a_balanced_catalog_with_out_of_stock_and_discounts()
    {
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: false).SeedAsync();

        var products = await _db.Db.Products.Include(p => p.Variants).ToListAsync();
        products.Should().HaveCount(8);
        products.Count(p => p.IsFeatured).Should().BeGreaterThan(0);
        products.Count(p => p.ProductDiscountPercentage > 0).Should().BeGreaterThan(0);
        products.Should().Contain(p => p.Variants.All(v => v.StockQuantity == 0)); // an out-of-stock product
        // 2 products per default category
        foreach (var slug in new[] { "rings", "necklaces", "earrings", "bracelets" })
        {
            var cat = await _db.Db.Categories.SingleAsync(c => c.SlugEn == slug);
            (await _db.Db.Products.CountAsync(p => p.CategoryId == cat.Id)).Should().Be(2);
        }
    }

    // --- Cloudinary media persistence ---

    [Fact]
    public async Task Media_seed_uploads_and_persists_urls_public_ids_and_alt_text()
    {
        var storage = new RecordingImageStorage();
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: true, storage: storage).SeedAsync();

        var categories = await _db.Db.Categories.ToListAsync();
        categories.Should().OnlyContain(c => c.ImageUrl != null && c.ImagePublicId != null && c.ImageAltEn != null && c.ImageAltAr != null);

        var products = await _db.Db.Products.Include(p => p.Images).ToListAsync();
        products.Should().OnlyContain(p => p.Images.Any(i => i.IsPrimary));
        products.SelectMany(p => p.Images).Should().OnlyContain(i => i.Url.StartsWith("https://") && i.PublicId != "" && i.AltEn != null);

        var hero = await _db.Db.HeroSections.SingleAsync();
        hero.ImageUrl.Should().StartWith("https://");
        hero.ImagePublicId.Should().NotBeNullOrWhiteSpace();

        storage.Folders.Should().Contain("novella/dev/categories");
        storage.Folders.Should().Contain("novella/dev/products");
        storage.Folders.Should().Contain("novella/dev/heroes");
    }

    [Fact]
    public async Task Media_seed_does_not_reupload_on_a_second_run()
    {
        var storage = new RecordingImageStorage();
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: true, storage: storage).SeedAsync();
        var firstRunUploads = storage.UploadCount;
        firstRunUploads.Should().BeGreaterThan(0);

        await CreateSeeder(devCatalog: true, cloudinaryConfigured: true, storage: storage).SeedAsync();
        storage.UploadCount.Should().Be(firstRunUploads); // no duplicate uploads
    }

    [Fact]
    public async Task Media_seed_is_skipped_gracefully_when_cloudinary_is_not_configured()
    {
        var storage = new RecordingImageStorage();
        await CreateSeeder(devCatalog: true, cloudinaryConfigured: false, storage: storage).SeedAsync();

        storage.UploadCount.Should().Be(0);
        (await _db.Db.Categories.CountAsync(c => c.ImageUrl != null)).Should().Be(0);
        (await _db.Db.HeroSections.CountAsync()).Should().Be(0);
        (await _db.Db.Products.CountAsync()).Should().Be(8); // catalog still seeded, just without images
    }

    // --- helpers ---

    // Each seed run uses a fresh context over the same connection, mirroring production's scoped context
    // and avoiding cross-run ChangeTracker contamination.
    private DataSeeder CreateSeeder(bool devCatalog, bool cloudinaryConfigured, IImageStorageProvider? storage = null)
    {
        var seed = Options.Create(new SeedOptions { AdminUsername = "admin", AdminPassword = "Admin@12345", EnableDevelopmentCatalog = devCatalog });
        var cloud = Options.Create(cloudinaryConfigured
            ? new CloudinaryOptions { CloudName = "demo", ApiKey = "key", ApiSecret = "secret" }
            : new CloudinaryOptions());
        return new DataSeeder(_db.NewContext(), TestSeed.Passwords, _clock, seed, cloud,
            storage ?? new RecordingImageStorage(), new FakeHostEnvironment(), NullLogger<DataSeeder>.Instance);
    }

    /// <summary>Creates 1-byte stand-in asset files so File.Exists passes in the test bin directory.</summary>
    private static void EnsureSeedAssets()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "SeedAssets");
        var files = new[]
        {
            "heroes/hero-1.webp",
            "categories/rings.webp", "categories/necklaces.webp", "categories/earrings.webp", "categories/bracelets.webp",
            "products/luna-ring.webp", "products/luna-ring-2.webp", "products/aurora-ring.webp",
            "products/story-necklace.webp", "products/grace-necklace.webp", "products/nova-earrings.webp",
            "products/petal-earrings.webp", "products/rose-bracelet.webp", "products/aurelia-bracelet.webp"
        };
        foreach (var rel in files)
        {
            var path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (!File.Exists(path)) File.WriteAllBytes(path, new byte[] { 0x52, 0x49, 0x46, 0x46 });
        }
    }

    private sealed class RecordingImageStorage : IImageStorageProvider
    {
        public int UploadCount;
        public List<string> Folders { get; } = new();

        public Task<ImageUploadResult> UploadAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
        {
            UploadCount++;
            Folders.Add(folder);
            return Task.FromResult(new ImageUploadResult(
                $"https://res.cloudinary.com/demo/{folder}/{fileName}",
                $"{folder}/{Path.GetFileNameWithoutExtension(fileName)}"));
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Novella.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
