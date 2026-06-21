using Novella.Application.Abstractions;

namespace Novella.Application.Payments;

public sealed record PaymentReadinessDto(
    string Method, string ProviderName, bool IsActive, string Environment,
    bool PublicKeyConfigured, bool SecretKeyConfigured, string? WebhookUrl, string ReadinessStatus);

/// <summary>Status-only payment readiness. Raw keys/secrets are never returned.</summary>
public sealed class PaymentAdminService
{
    private readonly IPaymentProviderFactory _factory;
    private readonly PaymentRuntimeOptions _options;

    public PaymentAdminService(IPaymentProviderFactory factory, PaymentRuntimeOptions options)
    {
        _factory = factory;
        _options = options;
    }

    public IReadOnlyList<PaymentReadinessDto> GetReadiness(string apiBaseUrl)
        => _factory.All.Select(provider =>
        {
            var isConfiguredProvider = string.Equals(_options.ActiveProvider, provider.ProviderName, StringComparison.OrdinalIgnoreCase);
            var hasWebhookSecret = !string.IsNullOrWhiteSpace(_options.WebhookSecret);
            var webhook = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : $"{apiBaseUrl.TrimEnd('/')}/api/payments/callback/{provider.ProviderName}";
            var readiness = provider.IsActive ? "Ready" : isConfiguredProvider ? "ConfiguredButInactive" : "EnvironmentNotConfigured";
            return new PaymentReadinessDto(provider.Method.ToString(), provider.ProviderName, provider.IsActive,
                isConfiguredProvider ? "Configured" : "Environment-managed",
                provider.IsActive || isConfiguredProvider,
                provider.IsActive || hasWebhookSecret,
                webhook,
                readiness);
        }).ToList();
}
