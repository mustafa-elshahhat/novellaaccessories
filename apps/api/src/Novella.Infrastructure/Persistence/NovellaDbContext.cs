using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Domain.Entities;

namespace Novella.Infrastructure.Persistence;

/// <summary>
/// EF Core context for all SQL Server business data. Holds NO WhatsApp/Baileys session state —
/// that lives only in apps/whatsapp's MongoDB.
/// </summary>
public class NovellaDbContext : DbContext, IAppDbContext
{
    public NovellaDbContext(DbContextOptions<NovellaDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerPhoneChangeRequest> CustomerPhoneChangeRequests => Set<CustomerPhoneChangeRequest>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<TwoOrderCouponSettings> TwoOrderCouponSettings => Set<TwoOrderCouponSettings>();

    public DbSet<ShippingGovernorate> ShippingGovernorates => Set<ShippingGovernorate>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<HeroSection> HeroSections => Set<HeroSection>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    public DbSet<WhatsAppSettings> WhatsAppSettings => Set<WhatsAppSettings>();
    public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs => Set<WhatsAppMessageLog>();
    public DbSet<ReminderSettings> ReminderSettings => Set<ReminderSettings>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();

    public DbSet<AnalyticsVisitor> AnalyticsVisitors => Set<AnalyticsVisitor>();
    public DbSet<AnalyticsSession> AnalyticsSessions => Set<AnalyticsSession>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All money is decimal(18,2) by default; (5,2) percentages are overridden per-property.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        ModelConfiguration.Apply(b);
    }
}
