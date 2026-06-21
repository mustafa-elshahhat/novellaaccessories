using Microsoft.Extensions.DependencyInjection;
using Novella.Application.Analytics;
using Novella.Application.Auth;
using Novella.Application.Cart;
using Novella.Application.Catalog;
using Novella.Application.Checkout;
using Novella.Application.Content;
using Novella.Application.Customers;
using Novella.Application.Discounts;
using Novella.Application.Expenses;
using Novella.Application.Orders;
using Novella.Application.Payments;
using Novella.Application.Reminders;
using Novella.Application.Reports;
using Novella.Application.Seo;
using Novella.Application.Shipping;
using Novella.Application.Uploads;
using Novella.Application.WhatsApp;

namespace Novella.Application;

/// <summary>Registers all Application use-case services (scoped).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<OtpService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AdminAuthService>();
        services.AddScoped<WhatsAppMessenger>();
        services.AddScoped<WhatsAppAdminService>();

        services.AddScoped<CatalogPublicService>();
        services.AddScoped<CatalogAdminService>();

        services.AddScoped<CouponService>();
        services.AddScoped<TwoOrderCouponService>();

        services.AddScoped<PricingAssembler>();
        services.AddScoped<CartService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<OrderService>();

        services.AddScoped<ShippingService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<PaymentAdminService>();
        services.AddScoped<UploadService>();

        services.AddScoped<AnalyticsService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<ReminderService>();

        services.AddScoped<SeoService>();
        services.AddScoped<ContentService>();
        services.AddScoped<ReportService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<CustomerAdminService>();

        return services;
    }
}
