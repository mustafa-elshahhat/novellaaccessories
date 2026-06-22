using Microsoft.EntityFrameworkCore;
using Novella.Domain.Entities;

namespace Novella.Infrastructure.Persistence;

/// <summary>
/// Central Fluent API configuration: enum-as-string, unique constraints, indexes, and the
/// decimal(5,2) percentage overrides. Keeps the schema aligned with docs/04_DATABASE_MODEL.md.
/// </summary>
internal static class ModelConfiguration
{
    public static void Apply(ModelBuilder b)
    {
        // ---- Accounts ----
        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.PhoneNumberNormalized).IsUnique();
            e.HasIndex(x => x.PhoneNumber).IsUnique();
        });

        b.Entity<CustomerPhoneChangeRequest>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => x.CustomerId);
        });

        b.Entity<AdminUser>(e => e.HasIndex(x => x.Username).IsUnique());

        b.Entity<OtpCode>(e =>
        {
            e.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.PhoneNumberNormalized, x.Purpose });
        });

        // ---- Catalog ----
        b.Entity<Category>(e =>
        {
            e.HasIndex(x => x.SlugAr).IsUnique();
            e.HasIndex(x => x.SlugEn).IsUnique();
            e.HasMany(x => x.Products).WithOne(x => x.Category!).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Product>(e =>
        {
            e.HasIndex(x => x.SlugAr).IsUnique();
            e.HasIndex(x => x.SlugEn).IsUnique();
            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.IsFeatured);
            e.Property(x => x.ProductDiscountPercentage).HasPrecision(5, 2);
            e.HasMany(x => x.Images).WithOne(x => x.Product!).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Variants).WithOne(x => x.Product!).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProductVariant>(e =>
        {
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<InventoryMovement>(e =>
        {
            e.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.OrderId);
            e.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Cart ----
        b.Entity<Cart>(e =>
        {
            e.HasIndex(x => x.CustomerId).IsUnique();
            e.HasMany(x => x.Items).WithOne(x => x.Cart!).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CartItem>(e =>
        {
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Orders ----
        b.Entity<Order>(e =>
        {
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => new { x.CustomerId, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
            e.HasIndex(x => new { x.CustomerId, x.CreatedAt });
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.DeliveredAt);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.IdempotencyKey).HasMaxLength(128);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(x => x.Order!).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ProductDiscountPercentage).HasPrecision(5, 2);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.ProductVariantId);
        });

        // ---- Coupons ----
        b.Entity<Coupon>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => new { x.CustomerId, x.Source })
                .IsUnique()
                .HasFilter("[CustomerId] IS NOT NULL AND [Source] = 'TwoDeliveredOrders'");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(50);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Usages).WithOne(x => x.Coupon!).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CouponUsage>(e =>
        {
            e.HasIndex(x => x.CouponId);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.OrderId);
        });

        b.Entity<TwoOrderCouponSettings>(e => e.Property(x => x.DiscountPercentage).HasPrecision(5, 2));

        // ---- Commerce ----
        b.Entity<ShippingGovernorate>(e => e.HasIndex(x => x.SortOrder));

        b.Entity<PaymentTransaction>(e =>
        {
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => x.OrderId);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Expense>(e =>
        {
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(100);
            e.HasIndex(x => x.ExpenseDate);
            e.HasIndex(x => x.Category);
        });

        // ---- Content ----
        b.Entity<HeroSection>(e => e.HasIndex(x => x.SortOrder));

        b.Entity<StaticPage>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => x.SlugAr).IsUnique();
            e.HasIndex(x => x.SlugEn).IsUnique();
        });

        // ---- Messaging ----
        b.Entity<WhatsAppMessageLog>(e =>
        {
            e.Property(x => x.MessageType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.CustomerId);
        });

        b.Entity<ReminderLog>(e =>
        {
            e.Property(x => x.ReminderType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.CustomerId, x.ReminderType });
        });

        // ---- Analytics ----
        b.Entity<AnalyticsVisitor>(e => e.HasIndex(x => x.AnonymousId).IsUnique());

        b.Entity<AnalyticsSession>(e =>
        {
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => x.UtmSource);
            e.HasOne(x => x.Visitor).WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AnalyticsEvent>(e =>
        {
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.MetadataJson).HasMaxLength(2048);
            e.HasIndex(x => new { x.EventType, x.CreatedAt });
            e.HasIndex(x => x.SessionId);
            e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
