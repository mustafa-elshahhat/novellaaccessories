using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
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
        await SeedDevelopmentCatalogAsync(now, ct);
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
                SortOrder = d.Sort, IsActive = true, CreatedAt = now,
                SeoTitleAr = $"{d.Ar} ناعمة من نوفيلا",
                SeoTitleEn = $"Novella {d.En}",
                SeoDescriptionAr = $"تشكيلة {d.Ar} أنيقة بتفاصيل هادئة مناسبة للإطلالات اليومية والمناسبات.",
                SeoDescriptionEn = $"Elegant {d.En.ToLowerInvariant()} with soft details for everyday looks and special moments.",
                AeoSummaryAr = $"اختاري {d.Ar} من نوفيلا حسب المقاس والستايل، مع توصيل داخل مصر ودعم عبر واتساب.",
                AeoSummaryEn = $"Choose Novella {d.En.ToLowerInvariant()} by size and style, with delivery in Egypt and WhatsApp support.",
                GeoContentAr = "نوفيلا أكسسوارات متجر مصري للإكسسوارات الناعمة بتصميم راق وخدمة محلية.",
                GeoContentEn = "Novella Accessories is an Egypt-based soft luxury accessories store with local delivery."
            });
        }
    }

    private async Task SeedDevelopmentCatalogAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.Products.AnyAsync(ct)) return;

        var categories = await _db.Categories.ToDictionaryAsync(c => c.SlugEn, ct);
        var samples = new[]
        {
            new { Category = "rings", NameAr = "خاتم لونا", NameEn = "Luna Ring", SlugAr = "khatam-luna", SlugEn = "luna-ring", Price = 420m, Cost = 210m, Discount = 15m },
            new { Category = "necklaces", NameAr = "سلسلة ستوري", NameEn = "Story Necklace", SlugAr = "silsila-story", SlugEn = "story-necklace", Price = 680m, Cost = 340m, Discount = 0m },
            new { Category = "earrings", NameAr = "حلق نوفا", NameEn = "Nova Earrings", SlugAr = "halaq-nova", SlugEn = "nova-earrings", Price = 360m, Cost = 180m, Discount = 10m },
            new { Category = "bracelets", NameAr = "أسورة روز", NameEn = "Rose Bracelet", SlugAr = "aswera-rose", SlugEn = "rose-bracelet", Price = 520m, Cost = 260m, Discount = 0m }
        };

        foreach (var s in samples)
        {
            if (!categories.TryGetValue(s.Category, out var category)) continue;
            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SlugAr = s.SlugAr,
                SlugEn = s.SlugEn,
                DescriptionAr = "قطعة ناعمة بتفاصيل دافئة، مناسبة كهدية أو لمسة يومية راقية.",
                DescriptionEn = "A soft accessory with warm details, made for gifting or an elegant everyday touch.",
                BasePurchasePrice = s.Cost,
                BaseSellingPrice = s.Price,
                ProductDiscountPercentage = s.Discount > 0 ? s.Discount : null,
                ProductDiscountStartAt = s.Discount > 0 ? now.AddDays(-7) : null,
                ProductDiscountEndAt = s.Discount > 0 ? now.AddDays(30) : null,
                IsFeatured = true,
                IsActive = true,
                SeoTitleAr = $"{s.NameAr} | نوفيلا أكسسوارات",
                SeoTitleEn = $"{s.NameEn} | Novella Accessories",
                SeoDescriptionAr = "إكسسوار أنيق بتصميم ناعم وتوصيل داخل مصر.",
                SeoDescriptionEn = "An elegant soft-luxury accessory with delivery in Egypt.",
                AeoSummaryAr = "هذه القطعة مناسبة للهدايا والإطلالات اليومية، ويمكن طلبها أونلاين والدفع عند الاستلام.",
                AeoSummaryEn = "This piece is suitable for gifting and everyday styling, with online ordering and cash on delivery.",
                GeoContentAr = "متاح للطلب من نوفيلا أكسسوارات داخل مصر حسب المحافظة المختارة.",
                GeoContentEn = "Available from Novella Accessories in Egypt with governorate-based delivery.",
                CreatedAt = now
            };

            product.Variants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Sku = $"NV-{s.SlugEn.ToUpperInvariant()}",
                NameAr = "قياسي",
                NameEn = "Standard",
                StockQuantity = 12,
                IsActive = true,
                CreatedAt = now
            });
            _db.Products.Add(product);
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
        var pages = new (string Key, string Ar, string En, string ContentAr, string ContentEn, string AeoAr, string AeoEn, string GeoAr, string GeoEn)[]
        {
            ("about", "من نحن", "About Us", "نوفيلا أكسسوارات علامة مصرية للإكسسوارات الناعمة. نختار قطعاً دافئة وبسيطة تضيف تفصيلاً راقياً لكل إطلالة.", "Novella Accessories is an Egypt-based soft luxury accessories brand. We curate warm, delicate pieces that add a refined detail to everyday style.", "نوفيلا تبيع خواتم وسلاسل وأقراط وأساور بتصميم هادئ وتوصيل داخل مصر.", "Novella sells rings, necklaces, earrings, and bracelets with a calm premium style and delivery in Egypt.", "متجر نوفيلا يخدم العملاء في مصر من خلال الطلب أونلاين والدعم عبر واتساب.", "Novella serves customers in Egypt through online ordering and WhatsApp support."),
            ("contact", "اتصل بنا", "Contact Us", "يمكنك التواصل معنا عبر واتساب لأي سؤال عن المنتجات أو الطلبات أو المقاسات. يسعدنا مساعدتك قبل وبعد الشراء.", "Contact us through WhatsApp for product questions, order support, or sizing guidance. We are happy to help before and after purchase.", "أفضل طريقة للتواصل مع نوفيلا هي واتساب للحصول على رد سريع حول الطلبات والمنتجات.", "The best way to contact Novella is WhatsApp for quick help with orders and products.", "دعم نوفيلا متاح للعملاء داخل مصر حسب أوقات العمل المعلنة.", "Novella support is available for customers in Egypt during published working hours."),
            ("privacy", "سياسة الخصوصية", "Privacy Policy", "نستخدم بياناتك فقط لتشغيل الحساب، تأكيد الطلب، التوصيل، وتحسين تجربة التسوق. لا نطلب بريدك الإلكتروني للتسجيل.", "We use your data only to run your account, confirm orders, deliver purchases, and improve shopping. Customer registration does not require email.", "نوفيلا تجمع رقم الهاتف والاسم والعنوان عند الطلب لتقديم الخدمة وإتمام التوصيل.", "Novella collects phone, name, and address data during ordering to provide service and delivery.", "تتعامل نوفيلا مع بيانات العملاء داخل نظامها التجاري ولا تبيع البيانات لأطراف خارجية.", "Novella handles customer data inside its commerce system and does not sell it to external parties."),
            ("terms", "الشروط والأحكام", "Terms & Conditions", "باستخدام متجر نوفيلا فأنت توافق على قواعد الطلب والدفع والشحن وسياسات التبديل والاسترجاع المنشورة في المتجر.", "By using Novella, you agree to the ordering, payment, shipping, return, and exchange rules published on the store.", "توضح شروط نوفيلا كيفية إتمام الطلبات، الدفع عند الاستلام، وحدود المسؤولية.", "Novella terms explain order placement, cash on delivery, and responsibility limits.", "تنطبق الشروط على الطلبات داخل مصر من خلال متجر نوفيلا الإلكتروني.", "These terms apply to orders in Egypt through the Novella online store."),
            ("returns", "الإرجاع والاستبدال", "Returns & Exchanges", "للاستبدال أو الاسترجاع، تواصلي معنا عبر واتساب مع رقم الطلب وصورة المنتج. تُراجع كل حالة حسب سياسة المتجر وحالة القطعة.", "For returns or exchanges, contact us on WhatsApp with the order number and product photo. Each case is reviewed according to store policy and item condition.", "طلبات الإرجاع والاستبدال في نوفيلا تتم عبر واتساب ولا يوجد نظام طلبات إرجاع داخلي في نسخة MVP.", "Novella handles returns and exchanges through WhatsApp; the MVP has no internal return-request module.", "سياسة الاسترجاع مخصصة لطلبات نوفيلا داخل مصر وتخضع لحالة المنتج ووقت التواصل.", "The return policy applies to Novella orders in Egypt and depends on item condition and contact timing."),
            ("shipping", "الشحن والتوصيل", "Shipping & Delivery", "يتم حساب الشحن حسب المحافظة المختارة أثناء الدفع. يظهر للعميل مبلغ الشحن الذي سيدفعه فقط، بينما تظل التكلفة الفعلية داخلية للتقارير.", "Shipping is calculated by the selected governorate during checkout. Customers see only the fee they pay; actual shipping cost remains internal for reports.", "نوفيلا توفر شحناً داخل المحافظات المصرية المتاحة في صفحة الدفع.", "Novella ships to active Egyptian governorates available at checkout.", "رسوم الشحن تعتمد على المحافظة داخل مصر ويتم حفظها مع الطلب وقت الإنشاء.", "Shipping fees depend on the governorate in Egypt and are snapshotted when the order is created."),
            ("faq", "الأسئلة الشائعة", "FAQ", "س: كيف أطلب من نوفيلا؟\nج: اختاري المنتج، أضيفيه للسلة، ثم أكملي بيانات الشحن والدفع.\n\nس: هل يوجد دفع عند الاستلام؟\nج: نعم، الدفع عند الاستلام متاح في النسخة الحالية.\n\nس: هل تظهر كمية المخزون؟\nج: لا، نعرض فقط ما إذا كان المنتج متاحاً أو غير متاح.\n\nس: كيف أتواصل للاستبدال؟\nج: تواصلي معنا عبر واتساب مع رقم الطلب.", "Q: How do I order from Novella?\nA: Choose a product, add it to cart, then complete shipping and payment details.\n\nQ: Is cash on delivery available?\nA: Yes, cash on delivery is available in the current version.\n\nQ: Do you show exact stock quantities?\nA: No, we only show whether an item is available or unavailable.\n\nQ: How do I request an exchange?\nA: Contact us on WhatsApp with your order number.", "نوفيلا تدعم الطلب أونلاين، الدفع عند الاستلام، الشحن حسب المحافظة، والتواصل عبر واتساب.", "Novella supports online ordering, cash on delivery, governorate-based shipping, and WhatsApp support.", "الأسئلة الشائعة تساعد عملاء نوفيلا في مصر على فهم الطلب والشحن والتواصل.", "The FAQ helps Novella customers in Egypt understand ordering, shipping, and support.")
        };

        foreach (var p in pages)
        {
            if (await _db.StaticPages.AnyAsync(x => x.Key == p.Key, ct)) continue;
            _db.StaticPages.Add(new StaticPage
            {
                Id = Guid.NewGuid(),
                Key = p.Key, TitleAr = p.Ar, TitleEn = p.En,
                SlugAr = p.Key, SlugEn = p.Key,
                ContentAr = p.ContentAr,
                ContentEn = p.ContentEn,
                SeoTitleAr = $"{p.Ar} | نوفيلا أكسسوارات",
                SeoTitleEn = $"{p.En} | Novella Accessories",
                SeoDescriptionAr = p.AeoAr,
                SeoDescriptionEn = p.AeoEn,
                AeoSummaryAr = p.AeoAr,
                AeoSummaryEn = p.AeoEn,
                GeoContentAr = p.GeoAr,
                GeoContentEn = p.GeoEn,
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
