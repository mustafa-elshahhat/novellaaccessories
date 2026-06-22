using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;
using Novella.Infrastructure.Configuration;

namespace Novella.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed. Core data (admin user, default categories, Egyptian governorates, static pages,
/// site settings, disabled WhatsApp/reminder/two-order settings) is seeded in all environments and is
/// safe to run repeatedly. Known old placeholder static-page rows are upgraded in place to full
/// bilingual content without overwriting admin edits. The development catalog (sample products) and
/// development imagery (hero/category/product images uploaded to Cloudinary) are gated behind
/// <see cref="SeedOptions.EnableDevelopmentCatalog"/> and, for images, a configured Cloudinary account.
/// </summary>
public sealed class DataSeeder
{
    private readonly NovellaDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IClock _clock;
    private readonly SeedOptions _seed;
    private readonly CloudinaryOptions _cloudinary;
    private readonly IImageStorageProvider _storage;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(NovellaDbContext db, IPasswordHasher passwords, IClock clock,
        IOptions<SeedOptions> seed, IOptions<CloudinaryOptions> cloudinary, IImageStorageProvider storage,
        IHostEnvironment env, ILogger<DataSeeder> logger)
    {
        _db = db;
        _passwords = passwords;
        _clock = clock;
        _seed = seed.Value;
        _cloudinary = cloudinary.Value;
        _storage = storage;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        // ---- Core data ----
        await SeedAdminAsync(ct);
        await SeedCategoriesAsync(now, ct);
        await SeedStaticPagesAsync(now, ct);
        await UpgradeStaticPagesAsync(now, ct);
        await SeedGovernoratesAsync(now, ct);
        await SeedSiteSettingsAsync(now, ct);
        await SeedWhatsAppSettingsAsync(now, ct);
        await SeedReminderSettingsAsync(now, ct);
        await SeedTwoOrderSettingsAsync(now, ct);
        // Persist core first so the development-catalog category lookup (a DB query) sees the categories.
        await _db.SaveChangesAsync(ct);

        // ---- Development catalog (gated) ----
        await SeedDevelopmentCatalogAsync(now, ct);
        await _db.SaveChangesAsync(ct);
        // Detach saved entities between phases: the media phase re-queries exactly what it needs, and
        // this avoids carrying tracked rows (e.g. variant concurrency tokens) across SaveChanges calls.
        _db.ChangeTracker.Clear();

        // ---- Development imagery (Cloudinary upload) — idempotent via DB null/empty checks ----
        await SeedDevelopmentMediaAsync(now, ct);
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

    // ---------- Categories ----------

    private static readonly (string Ar, string En, string SlugAr, string SlugEn, int Sort)[] DefaultCategories =
    {
        ("خواتم", "Rings", "khawatem", "rings", 1),
        ("سلاسل", "Necklaces", "salasel", "necklaces", 2),
        ("أقراط", "Earrings", "aqrat", "earrings", 3),
        ("أساور", "Bracelets", "asawer", "bracelets", 4)
    };

    private static void ApplyCategoryMetadata(Category c)
    {
        c.SeoTitleAr = $"{c.NameAr} ناعمة من نوفيلا";
        c.SeoTitleEn = $"Novella {c.NameEn}";
        c.SeoDescriptionAr = $"تشكيلة {c.NameAr} أنيقة بتفاصيل هادئة مناسبة للإطلالات اليومية والمناسبات.";
        c.SeoDescriptionEn = $"Elegant {c.NameEn.ToLowerInvariant()} with soft details for everyday looks and special moments.";
        c.AeoSummaryAr = $"اختاري {c.NameAr} من نوفيلا حسب المقاس والستايل، مع توصيل داخل مصر ودعم عبر واتساب.";
        c.AeoSummaryEn = $"Choose Novella {c.NameEn.ToLowerInvariant()} by size and style, with delivery in Egypt and WhatsApp support.";
        c.GeoContentAr = "نوفيلا أكسسوارات متجر مصري للإكسسوارات الناعمة بتصميم راق وخدمة محلية.";
        c.GeoContentEn = "Novella Accessories is an Egypt-based soft luxury accessories store with local delivery.";
    }

    private async Task SeedCategoriesAsync(DateTime now, CancellationToken ct)
    {
        foreach (var d in DefaultCategories)
        {
            var existing = await _db.Categories.FirstOrDefaultAsync(c => c.SlugEn == d.SlugEn, ct);
            if (existing is not null)
            {
                // Upgrade rows from an older seed that lacked SEO/AEO/GEO. Only fills when missing.
                if (string.IsNullOrWhiteSpace(existing.SeoTitleEn))
                {
                    ApplyCategoryMetadata(existing);
                    existing.UpdatedAt = now;
                }
                continue;
            }

            var c = new Category
            {
                Id = Guid.NewGuid(),
                NameAr = d.Ar, NameEn = d.En, SlugAr = d.SlugAr, SlugEn = d.SlugEn,
                SortOrder = d.Sort, IsActive = true, CreatedAt = now
            };
            ApplyCategoryMetadata(c);
            _db.Categories.Add(c);
        }
    }

    // ---------- Static pages ----------

    private sealed record PageSeed(
        string Key, string Ar, string En, string ContentAr, string ContentEn,
        string AeoAr, string AeoEn, string GeoAr, string GeoEn);

    private static PageSeed[] StaticPageSeeds() =>
    [
        new("about", "من نحن", "About Us",
            "نوفيلا أكسسوارات علامة مصرية للإكسسوارات الناعمة. نختار قطعاً دافئة وبسيطة تضيف تفصيلاً راقياً لكل إطلالة.",
            "Novella Accessories is an Egypt-based soft luxury accessories brand. We curate warm, delicate pieces that add a refined detail to everyday style.",
            "نوفيلا تبيع خواتم وسلاسل وأقراط وأساور بتصميم هادئ وتوصيل داخل مصر.",
            "Novella sells rings, necklaces, earrings, and bracelets with a calm premium style and delivery in Egypt.",
            "متجر نوفيلا يخدم العملاء في مصر من خلال الطلب أونلاين والدعم عبر واتساب.",
            "Novella serves customers in Egypt through online ordering and WhatsApp support."),
        new("contact", "اتصل بنا", "Contact Us",
            "يمكنك التواصل معنا عبر واتساب لأي سؤال عن المنتجات أو الطلبات أو المقاسات. يسعدنا مساعدتك قبل وبعد الشراء.",
            "Contact us through WhatsApp for product questions, order support, or sizing guidance. We are happy to help before and after purchase.",
            "أفضل طريقة للتواصل مع نوفيلا هي واتساب للحصول على رد سريع حول الطلبات والمنتجات.",
            "The best way to contact Novella is WhatsApp for quick help with orders and products.",
            "دعم نوفيلا متاح للعملاء داخل مصر حسب أوقات العمل المعلنة.",
            "Novella support is available for customers in Egypt during published working hours."),
        new("privacy", "سياسة الخصوصية", "Privacy Policy",
            "نستخدم بياناتك فقط لتشغيل الحساب، تأكيد الطلب، التوصيل، وتحسين تجربة التسوق. لا نطلب بريدك الإلكتروني للتسجيل.",
            "We use your data only to run your account, confirm orders, deliver purchases, and improve shopping. Customer registration does not require email.",
            "نوفيلا تجمع رقم الهاتف والاسم والعنوان عند الطلب لتقديم الخدمة وإتمام التوصيل.",
            "Novella collects phone, name, and address data during ordering to provide service and delivery.",
            "تتعامل نوفيلا مع بيانات العملاء داخل نظامها التجاري ولا تبيع البيانات لأطراف خارجية.",
            "Novella handles customer data inside its commerce system and does not sell it to external parties."),
        new("terms", "الشروط والأحكام", "Terms & Conditions",
            "باستخدام متجر نوفيلا فأنت توافق على قواعد الطلب والدفع والشحن وسياسات التبديل والاسترجاع المنشورة في المتجر.",
            "By using Novella, you agree to the ordering, payment, shipping, return, and exchange rules published on the store.",
            "توضح شروط نوفيلا كيفية إتمام الطلبات، الدفع عند الاستلام، وحدود المسؤولية.",
            "Novella terms explain order placement, cash on delivery, and responsibility limits.",
            "تنطبق الشروط على الطلبات داخل مصر من خلال متجر نوفيلا الإلكتروني.",
            "These terms apply to orders in Egypt through the Novella online store."),
        new("returns", "الإرجاع والاستبدال", "Returns & Exchanges",
            "للاستبدال أو الاسترجاع، تواصلي معنا عبر واتساب مع رقم الطلب وصورة المنتج. تُراجع كل حالة حسب سياسة المتجر وحالة القطعة.",
            "For returns or exchanges, contact us on WhatsApp with the order number and product photo. Each case is reviewed according to store policy and item condition.",
            "طلبات الإرجاع والاستبدال في نوفيلا تتم عبر واتساب ولا يوجد نظام طلبات إرجاع داخلي في نسخة MVP.",
            "Novella handles returns and exchanges through WhatsApp; the MVP has no internal return-request module.",
            "سياسة الاسترجاع مخصصة لطلبات نوفيلا داخل مصر وتخضع لحالة المنتج ووقت التواصل.",
            "The return policy applies to Novella orders in Egypt and depends on item condition and contact timing."),
        new("shipping", "الشحن والتوصيل", "Shipping & Delivery",
            "يتم حساب الشحن حسب المحافظة المختارة أثناء الدفع. يظهر للعميل مبلغ الشحن الذي سيدفعه قبل تأكيد الطلب، ويعتمد التوصيل على المحافظات المصرية المفعّلة. يظهر رقم التتبع عند توفره، ويمكنك التواصل معنا عبر واتساب لأي استفسار عن الشحنة.",
            "Shipping is calculated by the selected governorate during checkout. You see the shipping fee you will pay before confirming the order, and delivery depends on the active Egyptian governorates. A tracking number is shown when available, and you can reach us on WhatsApp for any delivery question.",
            "نوفيلا توفر شحناً داخل المحافظات المصرية المتاحة في صفحة الدفع، ويظهر مبلغ الشحن قبل تأكيد الطلب.",
            "Novella ships to active Egyptian governorates shown at checkout, and the shipping fee is shown before you confirm the order.",
            "رسوم الشحن تعتمد على المحافظة داخل مصر ويتم حفظها مع الطلب وقت الإنشاء.",
            "Shipping fees depend on the governorate in Egypt and are snapshotted when the order is created."),
        new("faq", "الأسئلة الشائعة", "FAQ",
            "س: كيف أطلب من نوفيلا؟\nج: اختاري المنتج، أضيفيه للسلة، ثم أكملي بيانات الشحن والدفع.\n\nس: هل يوجد دفع عند الاستلام؟\nج: نعم، الدفع عند الاستلام متاح في النسخة الحالية.\n\nس: كيف يتم حساب الشحن؟\nج: يُحسب الشحن حسب المحافظة المختارة ويظهر المبلغ قبل تأكيد الطلب.\n\nس: هل تظهر كمية المخزون؟\nج: لا، نعرض فقط ما إذا كان المنتج متاحاً أو غير متاح.\n\nس: كيف أتواصل للاستبدال؟\nج: تواصلي معنا عبر واتساب مع رقم الطلب.",
            "Q: How do I order from Novella?\nA: Choose a product, add it to cart, then complete shipping and payment details.\n\nQ: Is cash on delivery available?\nA: Yes, cash on delivery is available in the current version.\n\nQ: How is shipping calculated?\nA: Shipping is based on the selected governorate and the fee is shown before you confirm the order.\n\nQ: Do you show exact stock quantities?\nA: No, we only show whether an item is available or unavailable.\n\nQ: How do I request an exchange?\nA: Contact us on WhatsApp with your order number.",
            "نوفيلا تدعم الطلب أونلاين، الدفع عند الاستلام، الشحن حسب المحافظة، والتواصل عبر واتساب.",
            "Novella supports online ordering, cash on delivery, governorate-based shipping, and WhatsApp support.",
            "الأسئلة الشائعة تساعد عملاء نوفيلا في مصر على فهم الطلب والشحن والتواصل.",
            "The FAQ helps Novella customers in Egypt understand ordering, shipping, and support.")
    ];

    private async Task SeedStaticPagesAsync(DateTime now, CancellationToken ct)
    {
        foreach (var p in StaticPageSeeds())
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

    /// <summary>
    /// Safe upgrade of static pages seeded by an older build with placeholder content. A row is only
    /// upgraded when its stored content STILL EXACTLY MATCHES the known old placeholder strings
    /// (<c>"{En} page content (placeholder)."</c> / <c>"محتوى صفحة {Ar} (نص مؤقت)."</c>), so any
    /// admin-edited content is preserved. Missing SEO/AEO/GEO is also filled for those upgraded rows.
    /// </summary>
    private async Task UpgradeStaticPagesAsync(DateTime now, CancellationToken ct)
    {
        foreach (var p in StaticPageSeeds())
        {
            var page = await _db.StaticPages.FirstOrDefaultAsync(x => x.Key == p.Key, ct);
            if (page is null) continue;

            var oldPlaceholderEn = $"{p.En} page content (placeholder).";
            var oldPlaceholderAr = $"محتوى صفحة {p.Ar} (نص مؤقت).";
            var isPlaceholder = page.ContentEn == oldPlaceholderEn || page.ContentAr == oldPlaceholderAr;
            if (!isPlaceholder) continue;

            page.ContentAr = p.ContentAr;
            page.ContentEn = p.ContentEn;
            page.SeoTitleAr = $"{p.Ar} | نوفيلا أكسسوارات";
            page.SeoTitleEn = $"{p.En} | Novella Accessories";
            page.SeoDescriptionAr = p.AeoAr;
            page.SeoDescriptionEn = p.AeoEn;
            page.AeoSummaryAr = p.AeoAr;
            page.AeoSummaryEn = p.AeoEn;
            page.GeoContentAr = p.GeoAr;
            page.GeoContentEn = p.GeoEn;
            page.IsActive = true;
            page.UpdatedAt = now;
            _logger.LogInformation("Upgraded placeholder static page '{Key}' to full bilingual content.", p.Key);
        }
    }

    // ---------- Development catalog ----------

    private sealed record VariantSeed(string SkuSuffix, string? NameAr, string? NameEn, string? Size, int Stock);

    private sealed record ProductImageSeed(string Asset, bool IsPrimary, string AltAr, string AltEn);

    private sealed record ProductSeed(
        string CategorySlug, string NameAr, string NameEn, string SlugAr, string SlugEn,
        string DescAr, string DescEn, decimal Price, decimal Cost, decimal Discount, bool Featured,
        VariantSeed[] Variants, ProductImageSeed[] Images);

    private static ProductSeed[] DevelopmentProducts() =>
    [
        new("rings", "خاتم لونا", "Luna Ring", "khatam-luna", "luna-ring",
            "خاتم ناعم بطلاء ذهبي وردي ولمسة لامعة صغيرة، مثالي للإطلالة اليومية أو كهدية أنيقة.",
            "A delicate ring with a soft rose-gold finish and a subtle sparkle — perfect for everyday wear or an elegant gift.",
            420m, 210m, 15m, true,
            [new("", "قياسي", "Standard", null,12)],
            [new("luna-ring.webp", true, "خاتم لونا من نوفيلا أكسسوارات", "Luna Ring by Novella Accessories"),
             new("luna-ring-2.webp", false, "خاتم لونا - لقطة تفصيلية", "Luna Ring detail view")]),
        new("rings", "خاتم أورورا", "Aurora Ring", "khatam-aurora", "aurora-ring",
            "خاتم بتصميم متدرّج هادئ ولمسة شامبين، متوفر بأكثر من مقاس ليناسب إطلالتك.",
            "A ring with a calm graduated design and a champagne touch, available in more than one size to suit your look.",
            480m, 240m, 0m, false,
            [new("-6", "مقاس 6", "Size 6", "6", 8), new("-8", "مقاس 8", "Size 8", "8", 5)],
            [new("aurora-ring.webp", true, "خاتم أورورا من نوفيلا أكسسوارات", "Aurora Ring by Novella Accessories")]),
        new("necklaces", "سلسلة ستوري", "Story Necklace", "silsila-story", "story-necklace",
            "سلسلة رقيقة بدلاية صغيرة تحكي تفصيلة خاصة، مناسبة للطبقات أو للبس منفرد.",
            "A fine necklace with a small pendant that tells a quiet story — lovely layered or worn alone.",
            680m, 340m, 0m, true,
            [new("", "قياسي", "Standard", null,10)],
            [new("story-necklace.webp", true, "سلسلة ستوري من نوفيلا أكسسوارات", "Story Necklace by Novella Accessories")]),
        new("necklaces", "سلسلة جريس", "Grace Necklace", "silsila-grace", "grace-necklace",
            "سلسلة أنيقة بتشطيب ذهبي دافئ، خيار راقٍ للمناسبات والهدايا.",
            "An elegant necklace with a warm gold finish — a refined choice for occasions and gifting.",
            750m, 360m, 20m, true,
            [new("", "قياسي", "Standard", null,6)],
            [new("grace-necklace.webp", true, "سلسلة جريس من نوفيلا أكسسوارات", "Grace Necklace by Novella Accessories")]),
        new("earrings", "حلق نوفا", "Nova Earrings", "halaq-nova", "nova-earrings",
            "حلق خفيف بلمسة لامعة يضيف إشراقة ناعمة لإطلالتك اليومية.",
            "Lightweight earrings with a subtle sparkle that adds a soft glow to your everyday look.",
            360m, 180m, 10m, true,
            [new("", "قياسي", "Standard", null,14)],
            [new("nova-earrings.webp", true, "حلق نوفا من نوفيلا أكسسوارات", "Nova Earrings by Novella Accessories")]),
        new("earrings", "حلق بتلة", "Petal Earrings", "halaq-petal", "petal-earrings",
            "حلق مستوحى من تفاصيل الزهور بتصميم ناعم ودافئ (نفدت الكمية حالياً).",
            "Petal-inspired earrings with a soft, warm design (currently out of stock).",
            300m, 150m, 0m, false,
            [new("", "قياسي", "Standard", null,0)],
            [new("petal-earrings.webp", true, "حلق بتلة من نوفيلا أكسسوارات", "Petal Earrings by Novella Accessories")]),
        new("bracelets", "أسورة روز", "Rose Bracelet", "aswera-rose", "rose-bracelet",
            "أسورة ناعمة بلمسة ذهبية وردية تكمل إطلالتك بتفصيلة دافئة.",
            "A delicate bracelet with a rose-gold touch that completes your look with a warm detail.",
            520m, 260m, 0m, true,
            [new("", "قياسي", "Standard", null,9)],
            [new("rose-bracelet.webp", true, "أسورة روز من نوفيلا أكسسوارات", "Rose Bracelet by Novella Accessories")]),
        new("bracelets", "أسورة أوريليا", "Aurelia Bracelet", "aswera-aurelia", "aurelia-bracelet",
            "أسورة بتصميم بسيط وتشطيب شامبين، خفيفة ومريحة للبس اليومي.",
            "A minimal bracelet with a champagne finish — light and comfortable for everyday wear.",
            560m, 280m, 12m, false,
            [new("", "قياسي", "Standard", null,7)],
            [new("aurelia-bracelet.webp", true, "أسورة أوريليا من نوفيلا أكسسوارات", "Aurelia Bracelet by Novella Accessories")])
    ];

    private async Task SeedDevelopmentCatalogAsync(DateTime now, CancellationToken ct)
    {
        if (!_seed.EnableDevelopmentCatalog) return;

        var categories = await _db.Categories.ToDictionaryAsync(c => c.SlugEn, ct);

        foreach (var s in DevelopmentProducts())
        {
            // Per-slug idempotency: never duplicate, never overwrite an admin-edited product.
            if (await _db.Products.AnyAsync(p => p.SlugEn == s.SlugEn, ct)) continue;
            if (!categories.TryGetValue(s.CategorySlug, out var category)) continue;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                NameAr = s.NameAr, NameEn = s.NameEn,
                SlugAr = s.SlugAr, SlugEn = s.SlugEn,
                DescriptionAr = s.DescAr, DescriptionEn = s.DescEn,
                BasePurchasePrice = s.Cost,
                BaseSellingPrice = s.Price,
                ProductDiscountPercentage = s.Discount > 0 ? s.Discount : null,
                ProductDiscountStartAt = s.Discount > 0 ? now.AddDays(-7) : null,
                ProductDiscountEndAt = s.Discount > 0 ? now.AddDays(30) : null,
                IsFeatured = s.Featured,
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

            foreach (var v in s.Variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Sku = $"NV-{s.SlugEn.ToUpperInvariant()}{v.SkuSuffix}",
                    NameAr = v.NameAr, NameEn = v.NameEn, Size = v.Size,
                    StockQuantity = v.Stock,
                    IsActive = true,
                    CreatedAt = now
                });
            }
            _db.Products.Add(product);
        }
    }

    // ---------- Development imagery (Cloudinary) ----------

    private async Task SeedDevelopmentMediaAsync(DateTime now, CancellationToken ct)
    {
        if (!_seed.EnableDevelopmentCatalog) return;
        if (!_cloudinary.IsConfigured)
        {
            _logger.LogWarning("Development catalog enabled but Cloudinary is not configured; skipping image seeding. " +
                "Set Cloudinary__CloudName/ApiKey/ApiSecret to seed hero/category/product images.");
            return;
        }

        await SeedCategoryImagesAsync(now, ct);
        await _db.SaveChangesAsync(ct);
        await SeedProductImagesAsync(now, ct);
        await _db.SaveChangesAsync(ct);
        await SeedHeroAsync(now, ct);
        await _db.SaveChangesAsync(ct);
    }

    private static readonly (string SlugEn, string AltAr, string AltEn)[] CategoryImageAlts =
    {
        ("rings", "تشكيلة خواتم نوفيلا", "Novella rings collection"),
        ("necklaces", "تشكيلة سلاسل نوفيلا", "Novella necklaces collection"),
        ("earrings", "تشكيلة أقراط نوفيلا", "Novella earrings collection"),
        ("bracelets", "تشكيلة أساور نوفيلا", "Novella bracelets collection")
    };

    private async Task SeedCategoryImagesAsync(DateTime now, CancellationToken ct)
    {
        foreach (var (slug, altAr, altEn) in CategoryImageAlts)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.SlugEn == slug, ct);
            if (category is null || !string.IsNullOrWhiteSpace(category.ImageUrl)) continue; // idempotent

            var uploaded = await TryUploadAsync(Path.Combine("categories", $"{slug}.webp"), "novella/dev/categories", ct);
            if (uploaded is null) continue;
            category.ImageUrl = uploaded.Url;
            category.ImagePublicId = uploaded.PublicId;
            category.ImageAltAr = altAr;
            category.ImageAltEn = altEn;
            category.UpdatedAt = now;
        }
    }

    private async Task SeedProductImagesAsync(DateTime now, CancellationToken ct)
    {
        // Project to ids only (no graph tracking) and add image rows directly to the set as clean inserts.
        var products = await _db.Products
            .Select(p => new { p.Id, p.SlugEn, HasImages = p.Images.Any() })
            .ToListAsync(ct);

        foreach (var s in DevelopmentProducts())
        {
            var product = products.FirstOrDefault(p => p.SlugEn == s.SlugEn);
            if (product is null || product.HasImages) continue; // idempotent: only when no images yet

            var sort = 0;
            foreach (var img in s.Images)
            {
                var uploaded = await TryUploadAsync(Path.Combine("products", img.Asset), "novella/dev/products", ct);
                if (uploaded is null) continue;
                _db.ProductImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Url = uploaded.Url,
                    PublicId = uploaded.PublicId,
                    AltAr = img.AltAr,
                    AltEn = img.AltEn,
                    SortOrder = sort++,
                    IsPrimary = img.IsPrimary,
                    CreatedAt = now
                });
            }
        }
    }

    private async Task SeedHeroAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.HeroSections.AnyAsync(ct)) return; // idempotent: only seed when none exist

        var uploaded = await TryUploadAsync(Path.Combine("heroes", "hero-1.webp"), "novella/dev/heroes", ct);
        if (uploaded is null) return; // hero requires an image; never seed an imageless hero

        _db.HeroSections.Add(new HeroSection
        {
            Id = Guid.NewGuid(),
            ImageUrl = uploaded.Url,
            ImagePublicId = uploaded.PublicId,
            TitleAr = "إكسسوارات تحكي قصتك",
            TitleEn = "Jewelry That Tells Your Story",
            SubtitleAr = "قطع ناعمة فاخرة بتفاصيل دافئة، مع توصيل داخل مصر.",
            SubtitleEn = "Soft luxury pieces with warm details, delivered across Egypt.",
            CtaTextAr = "تسوّقي التشكيلة",
            CtaTextEn = "Shop the collection",
            CtaLink = "/products",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = now
        });
    }

    /// <summary>Uploads a SeedAssets file to Cloudinary; returns null (and logs) if the file is missing.</summary>
    private async Task<ImageUploadResult?> TryUploadAsync(string relativeAssetPath, string folder, CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SeedAssets", relativeAssetPath);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Seed image asset not found, skipping: {Path}", path);
            return null;
        }
        await using var stream = File.OpenRead(path);
        return await _storage.UploadAsync(stream, Path.GetFileName(path), folder, ct);
    }

    // ---------- Governorates, settings ----------

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
