using Microsoft.EntityFrameworkCore;
using Novella.Domain.Entities;

namespace Novella.Application.Abstractions;

/// <summary>
/// Application-facing view of the persistence context. Infrastructure provides the EF Core
/// implementation; Application/Domain never reference a concrete database.
/// </summary>
public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<CustomerPhoneChangeRequest> CustomerPhoneChangeRequests { get; }
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<OtpCode> OtpCodes { get; }

    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }

    DbSet<Novella.Domain.Entities.Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }

    DbSet<Coupon> Coupons { get; }
    DbSet<CouponUsage> CouponUsages { get; }
    DbSet<TwoOrderCouponSettings> TwoOrderCouponSettings { get; }

    DbSet<ShippingGovernorate> ShippingGovernorates { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<Expense> Expenses { get; }

    DbSet<HeroSection> HeroSections { get; }
    DbSet<StaticPage> StaticPages { get; }
    DbSet<ShippingSettings> ShippingSettings { get; }

    DbSet<WhatsAppSettings> WhatsAppSettings { get; }
    DbSet<WhatsAppMessageLog> WhatsAppMessageLogs { get; }
    DbSet<ReminderSettings> ReminderSettings { get; }
    DbSet<ReminderLog> ReminderLogs { get; }

    DbSet<AnalyticsVisitor> AnalyticsVisitors { get; }
    DbSet<AnalyticsSession> AnalyticsSessions { get; }
    DbSet<AnalyticsEvent> AnalyticsEvents { get; }

    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
