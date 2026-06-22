using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Novella.Application.Abstractions;
using Novella.Application.Payments;
using Novella.Application.WhatsApp;
using Novella.Infrastructure.BackgroundJobs;
using Novella.Infrastructure.Configuration;
using Novella.Infrastructure.Payments;
using Novella.Infrastructure.Persistence;
using Novella.Infrastructure.Security;
using Novella.Infrastructure.Shipping;
using Novella.Infrastructure.Storage;
using Novella.Infrastructure.WhatsApp;

namespace Novella.Infrastructure;

/// <summary>Registers persistence, providers, security services, and background jobs.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- Options ----
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<CloudinaryOptions>(config.GetSection(CloudinaryOptions.Section));
        services.Configure<WhatsAppOptions>(config.GetSection(WhatsAppOptions.Section));
        services.Configure<PaymentOptions>(config.GetSection(PaymentOptions.Section));
        services.Configure<SeedOptions>(config.GetSection(SeedOptions.Section));

        // ---- Persistence ----
        var connectionString = config.GetConnectionString("DefaultConnection");
        services.AddDbContext<NovellaDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure();
                    sql.CommandTimeout(0);
                });
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<NovellaDbContext>());
        services.AddScoped<DataSeeder>();

        // ---- Security ----
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IOtpHasher, OtpHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // ---- Providers ----
        services.AddScoped<IImageStorageProvider, CloudinaryImageStorageProvider>();
        services.AddSingleton<IWhatsAppConfigStatus, WhatsAppConfigStatus>();

        services.AddHttpClient<IWhatsAppClient, WhatsAppClient>(c => c.Timeout = TimeSpan.FromSeconds(35));

        // Payment providers (COD active; gateways prepared but inactive).
        services.AddScoped<IPaymentProvider, CashOnDeliveryPaymentProvider>();
        services.AddScoped<IPaymentProvider, BankCardPaymentProvider>();
        services.AddScoped<IPaymentProvider, InstapayPaymentProvider>();
        services.AddScoped<IPaymentProvider, WalletPaymentProvider>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddSingleton(sp =>
        {
            var payment = config.GetSection(PaymentOptions.Section).Get<PaymentOptions>() ?? new PaymentOptions();
            return new PaymentRuntimeOptions(payment.WebhookSecret, payment.ActiveProvider);
        });

        // Shipping (manual MVP).
        services.AddScoped<IShippingProvider, ManualShippingProvider>();

        // ---- Background jobs ----
        services.AddHostedService<ReminderBackgroundService>();

        return services;
    }
}
