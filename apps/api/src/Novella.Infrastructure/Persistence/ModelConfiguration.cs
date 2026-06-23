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
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.PhoneNumberNormalized).HasMaxLength(30);
            e.HasIndex(x => x.PhoneNumberNormalized).IsUnique();
            e.HasIndex(x => x.PhoneNumber).IsUnique();
        });

        b.Entity<CustomerPhoneChangeRequest>(e =>
        {
            e.Property(x => x.OldPhoneNumber).HasMaxLength(30);
            e.Property(x => x.NewPhoneNumber).HasMaxLength(30);
            e.Property(x => x.NewPhoneNumberNormalized).HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => x.CustomerId);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AdminUser>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => x.Username).IsUnique();
        });

        b.Entity<OtpCode>(e =>
        {
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.PhoneNumberNormalized).HasMaxLength(30);
            e.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.PhoneNumberNormalized, x.Purpose });
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.RelatedCustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Catalog ----
        b.Entity<Category>(e =>
        {
            e.Property(x => x.NameAr).HasMaxLength(200);
            e.Property(x => x.NameEn).HasMaxLength(200);
            e.Property(x => x.SlugAr).HasMaxLength(220);
            e.Property(x => x.SlugEn).HasMaxLength(220);
            e.Property(x => x.ImagePublicId).HasMaxLength(300);
            e.Property(x => x.ImageAltAr).HasMaxLength(300);
            e.Property(x => x.ImageAltEn).HasMaxLength(300);
            e.Property(x => x.SeoTitleAr).HasMaxLength(300);
            e.Property(x => x.SeoTitleEn).HasMaxLength(300);
            e.Property(x => x.SeoDescriptionAr).HasMaxLength(500);
            e.Property(x => x.SeoDescriptionEn).HasMaxLength(500);
            e.HasIndex(x => x.SlugAr).IsUnique();
            e.HasIndex(x => x.SlugEn).IsUnique();
            e.HasMany(x => x.Products).WithOne(x => x.Category!).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Product>(e =>
        {
            e.Property(x => x.NameAr).HasMaxLength(300);
            e.Property(x => x.NameEn).HasMaxLength(300);
            e.Property(x => x.SlugAr).HasMaxLength(320);
            e.Property(x => x.SlugEn).HasMaxLength(320);
            e.Property(x => x.SeoTitleAr).HasMaxLength(300);
            e.Property(x => x.SeoTitleEn).HasMaxLength(300);
            e.Property(x => x.SeoDescriptionAr).HasMaxLength(500);
            e.Property(x => x.SeoDescriptionEn).HasMaxLength(500);
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
            e.Property(x => x.Sku).HasMaxLength(100);
            e.Property(x => x.NameAr).HasMaxLength(300);
            e.Property(x => x.NameEn).HasMaxLength(300);
            e.Property(x => x.Size).HasMaxLength(100);
            e.Property(x => x.ColorAr).HasMaxLength(100);
            e.Property(x => x.ColorEn).HasMaxLength(100);
            e.Property(x => x.MaterialAr).HasMaxLength(100);
            e.Property(x => x.MaterialEn).HasMaxLength(100);
            e.Property(x => x.CustomOptionNameAr).HasMaxLength(100);
            e.Property(x => x.CustomOptionNameEn).HasMaxLength(100);
            e.Property(x => x.CustomOptionValueAr).HasMaxLength(100);
            e.Property(x => x.CustomOptionValueEn).HasMaxLength(100);
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<InventoryMovement>(e =>
        {
            e.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => x.ProductVariantId);
            e.HasIndex(x => x.OrderId);
            e.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AdminUser>().WithMany().HasForeignKey(x => x.CreatedByAdminId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Cart ----
        b.Entity<Cart>(e =>
        {
            e.HasIndex(x => x.CustomerId).IsUnique();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
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
            e.Property(x => x.OrderNumber).HasMaxLength(50);
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.Property(x => x.CustomerPhone).HasMaxLength(30);
            e.Property(x => x.GovernorateNameAr).HasMaxLength(200);
            e.Property(x => x.GovernorateNameEn).HasMaxLength(200);
            e.Property(x => x.CityDistrict).HasMaxLength(200);
            e.Property(x => x.ShippingProviderName).HasMaxLength(100);
            e.Property(x => x.ExternalTrackingNumber).HasMaxLength(200);
            e.Property(x => x.ExternalShippingStatus).HasMaxLength(200);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ShippingGovernorate>().WithMany().HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(x => x.Order!).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ProductNameAr).HasMaxLength(300);
            e.Property(x => x.ProductNameEn).HasMaxLength(300);
            e.Property(x => x.VariantNameAr).HasMaxLength(300);
            e.Property(x => x.VariantNameEn).HasMaxLength(300);
            e.Property(x => x.Sku).HasMaxLength(100);
            e.Property(x => x.ProductDiscountPercentage).HasPrecision(5, 2);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.ProductVariantId);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Coupons ----
        b.Entity<Coupon>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(100);
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
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TwoOrderCouponSettings>(e => e.Property(x => x.DiscountPercentage).HasPrecision(5, 2));

        // ---- Commerce ----
        b.Entity<ShippingGovernorate>(e =>
        {
            e.Property(x => x.NameAr).HasMaxLength(200);
            e.Property(x => x.NameEn).HasMaxLength(200);
            e.HasIndex(x => x.SortOrder);
        });

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
            e.Property(x => x.RelatedCampaignName).HasMaxLength(200);
            e.HasIndex(x => x.ExpenseDate);
            e.HasIndex(x => x.Category);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.RelatedOrderId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Content ----
        b.Entity<HeroSection>(e =>
        {
            e.Property(x => x.ImagePublicId).HasMaxLength(300);
            e.Property(x => x.TitleAr).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.Property(x => x.CtaTextAr).HasMaxLength(100);
            e.Property(x => x.CtaTextEn).HasMaxLength(100);
            e.HasIndex(x => x.SortOrder);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.LinkedProductId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<StaticPage>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.TitleAr).HasMaxLength(300);
            e.Property(x => x.TitleEn).HasMaxLength(300);
            e.Property(x => x.SlugAr).HasMaxLength(300);
            e.Property(x => x.SlugEn).HasMaxLength(300);
            e.Property(x => x.SeoTitleAr).HasMaxLength(300);
            e.Property(x => x.SeoTitleEn).HasMaxLength(300);
            e.Property(x => x.SeoDescriptionAr).HasMaxLength(500);
            e.Property(x => x.SeoDescriptionEn).HasMaxLength(500);
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => x.SlugAr).IsUnique();
            e.HasIndex(x => x.SlugEn).IsUnique();
        });

        // ---- Messaging ----
        b.Entity<WhatsAppMessageLog>(e =>
        {
            e.Property(x => x.PhoneNumber).HasMaxLength(30);
            e.Property(x => x.MessageType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.TemplateKey).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.CustomerId);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReminderLog>(e =>
        {
            e.Property(x => x.ReminderType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => new { x.CustomerId, x.ReminderType });
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Cart>().WithMany().HasForeignKey(x => x.RelatedCartId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<AnalyticsSession>().WithMany().HasForeignKey(x => x.RelatedVisitSessionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<WhatsAppMessageLog>().WithMany().HasForeignKey(x => x.WhatsAppMessageLogId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---- Analytics ----
        b.Entity<AnalyticsVisitor>(e =>
        {
            e.Property(x => x.AnonymousId).HasMaxLength(100);
            e.HasIndex(x => x.AnonymousId).IsUnique();
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<AnalyticsSession>(e =>
        {
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => x.UtmSource);
            e.HasOne(x => x.Visitor).WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.ConvertedOrderId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<AnalyticsEvent>(e =>
        {
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(100);
            e.Property(x => x.MetadataJson).HasMaxLength(2048);
            e.HasIndex(x => new { x.EventType, x.CreatedAt });
            e.HasIndex(x => x.SessionId);
            e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<AnalyticsVisitor>().WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ProductImage>(e =>
        {
            e.Property(x => x.PublicId).HasMaxLength(300);
            e.Property(x => x.AltAr).HasMaxLength(300);
            e.Property(x => x.AltEn).HasMaxLength(300);
        });

        b.Entity<ShippingSettings>();
    }
}
