using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Infrastructure.Persistence;
using Novella.Infrastructure.Security;

namespace Novella.Tests;

/// <summary>A disposable in-memory SQLite database for integration-style service tests.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    public NovellaDbContext Db { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<NovellaDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new NovellaDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Deterministic, advanceable clock.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime? start = null) => UtcNow = start ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public DateTime UtcNow { get; set; }
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

/// <summary>Records WhatsApp send attempts; configurable to succeed or fail.</summary>
public sealed class FakeWhatsAppClient : IWhatsAppClient
{
    public bool ShouldSucceed { get; set; } = true;
    public bool Reachable { get; set; } = true;
    public bool Connected { get; set; } = true;
    public List<(string Phone, string Message)> Sends { get; } = new();

    public Task<WhatsAppSendResult> SendMessageAsync(string phone, string message, CancellationToken ct = default)
    {
        Sends.Add((phone, message));
        return Task.FromResult(ShouldSucceed
            ? new WhatsAppSendResult(true, "msg-id", null, false)
            : new WhatsAppSendResult(false, null, "send_failed", true));
    }

    public Task<WhatsAppStatusResult> GetStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new WhatsAppStatusResult(Reachable, Connected, "{}", null));
}

public sealed class FakeWhatsAppConfigStatus : Application.WhatsApp.IWhatsAppConfigStatus
{
    public bool IsConfigured { get; set; } = true;
}

/// <summary>Shared seed helpers for tests.</summary>
public static class TestSeed
{
    public static IPasswordHasher Passwords { get; } = new BcryptPasswordHasher();
    public static IOtpHasher OtpHasher { get; } = new OtpHasher();

    public static Customer AddCustomer(NovellaDbContext db, FakeClock clock, string phone = "201000000001", bool verified = true)
    {
        var c = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Test Customer",
            PhoneNumber = phone,
            PhoneNumberNormalized = phone,
            PasswordHash = Passwords.Hash("Password1"),
            IsPhoneVerified = verified,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };
        db.Customers.Add(c);
        db.SaveChanges();
        return c;
    }

    public static (Product product, ProductVariant variant) AddProduct(
        NovellaDbContext db, FakeClock clock, decimal sellingPrice = 1000m, decimal purchasePrice = 600m,
        int stock = 10, decimal? discountPct = null, bool active = true)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(), NameAr = "فئة", NameEn = "Category",
            SlugAr = "cat-" + Guid.NewGuid().ToString("N")[..6], SlugEn = "cat-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = true, CreatedAt = clock.UtcNow
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(), CategoryId = category.Id,
            NameAr = "منتج", NameEn = "Product",
            SlugAr = "p-" + Guid.NewGuid().ToString("N")[..6], SlugEn = "p-" + Guid.NewGuid().ToString("N")[..6],
            BasePurchasePrice = purchasePrice, BaseSellingPrice = sellingPrice,
            ProductDiscountPercentage = discountPct,
            ProductDiscountStartAt = discountPct is null ? null : clock.UtcNow.AddDays(-1),
            ProductDiscountEndAt = discountPct is null ? null : clock.UtcNow.AddDays(1),
            IsActive = active, CreatedAt = clock.UtcNow
        };
        db.Products.Add(product);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id,
            Sku = "SKU-" + Guid.NewGuid().ToString("N")[..8],
            StockQuantity = stock, IsActive = active, CreatedAt = clock.UtcNow
        };
        db.ProductVariants.Add(variant);
        db.SaveChanges();
        return (product, variant);
    }

    public static ShippingGovernorate AddGovernorate(NovellaDbContext db, FakeClock clock, decimal fee = 50m, decimal cost = 35m, bool active = true)
    {
        var g = new ShippingGovernorate
        {
            Id = Guid.NewGuid(), NameAr = "القاهرة", NameEn = "Cairo",
            CustomerPaidShippingFee = fee, ActualShippingCost = cost, IsActive = active, SortOrder = 1, CreatedAt = clock.UtcNow
        };
        db.ShippingGovernorates.Add(g);
        db.SaveChanges();
        return g;
    }

    public static void EnableWhatsApp(NovellaDbContext db, FakeClock clock, bool enabled = true)
    {
        db.WhatsAppSettings.Add(new WhatsAppSettings { Id = Guid.NewGuid(), IsEnabled = enabled, TransportName = "BaileysWhatsAppWeb", UpdatedAt = clock.UtcNow });
        db.SaveChanges();
    }
}
