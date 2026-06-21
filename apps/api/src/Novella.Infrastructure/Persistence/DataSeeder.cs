using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Infrastructure.Configuration;

namespace Novella.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed: admin user (password from config — never hardcoded), default categories,
/// Egyptian governorates, static pages, site settings, and disabled WhatsApp/reminder/two-order
/// settings. Safe to run repeatedly.
/// </summary>
public sealed class DataSeeder
{
    private readonly NovellaDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IClock _clock;
    private readonly SeedOptions _seed;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(NovellaDbContext db, IPasswordHasher passwords, IClock clock,
        IOptions<SeedOptions> seed, IHostEnvironment env, ILogger<DataSeeder> logger)
    {
        _db = db;
        _passwords = passwords;
        _clock = clock;
        _seed = seed.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        await SeedAdminAsync(ct);
        await SeedCategoriesAsync(now, ct);
        await SeedGovernoratesAsync(now, ct);
        await SeedStaticPagesAsync(now, ct);
        await SeedSiteSettingsAsync(now, ct);
        await SeedWhatsAppSettingsAsync(now, ct);
        await SeedReminderSettingsAsync(now, ct);
        await SeedTwoOrderSettingsAsync(now, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (await _db.AdminUsers.AnyAsync(a => a.Username == _seed.AdminUsername, ct)) return;

        string password;
        if (!string.IsNullOrWhiteSpace(_seed.AdminPassword))
        {
            password = _seed.AdminPassword!;
        }
        else if (_env.IsDevelopment())
        {
            password = "Admin@12345";
            _logger.LogWarning("Seed__AdminPassword not set; using a development default. Set it before production.");
        }
        else
        {
            // Never seed a weak default in production. Create an unusable random hash; require a real reset.
            password = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            _logger.LogWarning("Seed__AdminPassword not set in production; admin created with a random password. Set the password and reset.");
        }

        _db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = _seed.AdminUsername,
            DisplayName = _seed.AdminDisplayName,
            PasswordHash = _passwords.Hash(password),
            IsActive = true,
            CreatedAt = _clock.UtcNow
        });
    }

    private async Task SeedCategoriesAsync(DateTime now, CancellationToken ct)
    {
        var defaults = new (string Ar, string En, string SlugAr, string SlugEn, int Sort)[]
        {
            ("خواتم", "Rings", "khawatem", "rings", 1),
            ("سلاسل", "Necklaces", "salasel", "necklaces", 2),
            ("أقراط", "Earrings", "aqrat", "earrings", 3),
            ("أساور", "Bracelets", "asawer", "bracelets", 4)
        };

        foreach (var d in defaults)
        {
            if (await _db.Categories.AnyAsync(c => c.SlugEn == d.SlugEn, ct)) continue;
            _db.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                NameAr = d.Ar, NameEn = d.En, SlugAr = d.SlugAr, SlugEn = d.SlugEn,
                SortOrder = d.Sort, IsActive = true, CreatedAt = now
            });
        }
    }

    private async Task SeedGovernoratesAsync(DateTime now, CancellationToken ct)
    {
        // Egyptian governorates with placeholder customer-paid fee and actual cost.
        var govs = new (string Ar, string En, decimal Fee, decimal Cost)[]
        {
            ("القاهرة", "Cairo", 50m, 35m), ("الجيزة", "Giza", 50m, 35m), ("الإسكندرية", "Alexandria", 60m, 45m),
            ("القليوبية", "Qalyubia", 55m, 40m), ("الدقهلية", "Dakahlia", 65m, 50m), ("الشرقية", "Sharqia", 65m, 50m),
            ("الغربية", "Gharbia", 60m, 45m), ("المنوفية", "Monufia", 60m, 45m), ("كفر الشيخ", "Kafr El Sheikh", 70m, 55m),
            ("البحيرة", "Beheira", 70m, 55m), ("دمياط", "Damietta", 70m, 55m), ("بورسعيد", "Port Said", 75m, 60m),
            ("الإسماعيلية", "Ismailia", 70m, 55m), ("السويس", "Suez", 70m, 55m), ("الفيوم", "Faiyum", 70m, 55m),
            ("بني سويف", "Beni Suef", 75m, 60m), ("المنيا", "Minya", 80m, 65m), ("أسيوط", "Asyut", 85m, 70m),
            ("سوهاج", "Sohag", 90m, 75m), ("قنا", "Qena", 95m, 80m), ("الأقصر", "Luxor", 100m, 85m),
            ("أسوان", "Aswan", 110m, 95m), ("البحر الأحمر", "Red Sea", 110m, 95m), ("الوادي الجديد", "New Valley", 120m, 105m),
            ("مطروح", "Matrouh", 110m, 95m), ("شمال سيناء", "North Sinai", 120m, 105m), ("جنوب سيناء", "South Sinai", 120m, 105m)
        };

        var sort = 1;
        foreach (var g in govs)
        {
            if (await _db.ShippingGovernorates.AnyAsync(x => x.NameEn == g.En, ct)) { sort++; continue; }
            _db.ShippingGovernorates.Add(new ShippingGovernorate
            {
                Id = Guid.NewGuid(),
                NameAr = g.Ar, NameEn = g.En,
                CustomerPaidShippingFee = g.Fee, ActualShippingCost = g.Cost,
                IsActive = true, SortOrder = sort++, CreatedAt = now
            });
        }
    }

    private async Task SeedStaticPagesAsync(DateTime now, CancellationToken ct)
    {
        var pages = new (string Key, string Ar, string En)[]
        {
            ("about", "من نحن", "About Us"),
            ("contact", "اتصل بنا", "Contact Us"),
            ("privacy", "سياسة الخصوصية", "Privacy Policy"),
            ("terms", "الشروط والأحكام", "Terms & Conditions"),
            ("returns", "الإرجاع والاستبدال", "Returns & Exchanges"),
            ("shipping", "الشحن والتوصيل", "Shipping & Delivery"),
            ("faq", "الأسئلة الشائعة", "FAQ")
        };

        foreach (var p in pages)
        {
            if (await _db.StaticPages.AnyAsync(x => x.Key == p.Key, ct)) continue;
            _db.StaticPages.Add(new StaticPage
            {
                Id = Guid.NewGuid(),
                Key = p.Key, TitleAr = p.Ar, TitleEn = p.En,
                SlugAr = p.Key, SlugEn = p.Key,
                ContentAr = $"محتوى صفحة {p.Ar} (نص مؤقت).",
                ContentEn = $"{p.En} page content (placeholder).",
                IsActive = true, UpdatedAt = now
            });
        }
    }

    private async Task SeedSiteSettingsAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.SiteSettings.AnyAsync(ct)) return;
        _db.SiteSettings.Add(new SiteSettings
        {
            Id = Guid.NewGuid(),
            SiteNameAr = "نوفيلا أكسسوارات",
            SiteNameEn = "Novella Accessories",
            Domain = "novellaaccessories.store",
            DefaultSeoTitleAr = "نوفيلا أكسسوارات",
            DefaultSeoTitleEn = "Novella Accessories",
            DefaultSeoDescriptionAr = "إكسسوارات أنيقة بجودة عالية.",
            DefaultSeoDescriptionEn = "Elegant, high-quality accessories.",
            IsFreeShippingEnabled = false,
            UpdatedAt = now
        });
    }

    private async Task SeedWhatsAppSettingsAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.WhatsAppSettings.AnyAsync(ct)) return;
        _db.WhatsAppSettings.Add(new WhatsAppSettings
        {
            Id = Guid.NewGuid(),
            IsEnabled = false,
            TransportName = "BaileysWhatsAppWeb",
            UpdatedAt = now
        });
    }

    private async Task SeedReminderSettingsAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.ReminderSettings.AnyAsync(ct)) return;
        _db.ReminderSettings.Add(new ReminderSettings
        {
            Id = Guid.NewGuid(),
            AbandonedCheckoutEnabled = false, AbandonedCheckoutDelayHours = 4,
            InactiveCustomerEnabled = false, InactiveCustomerDelayDays = 30,
            UpdatedAt = now
        });
    }

    private async Task SeedTwoOrderSettingsAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.TwoOrderCouponSettings.AnyAsync(ct)) return;
        _db.TwoOrderCouponSettings.Add(new TwoOrderCouponSettings
        {
            Id = Guid.NewGuid(),
            IsEnabled = false, DiscountPercentage = 10m, ValidityDays = 30, SendWhatsAppMessage = false,
            UpdatedAt = now
        });
    }
}
