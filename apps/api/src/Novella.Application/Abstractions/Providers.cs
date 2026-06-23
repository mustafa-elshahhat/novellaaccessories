using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Abstractions;

/// <summary>Result of a WhatsApp send attempt via the apps/whatsapp sidecar.</summary>
public sealed record WhatsAppSendResult(bool Success, string? ProviderMessageId, string? Error, bool Retryable);

/// <summary>Connection/session status proxied from the sidecar's protected /status endpoint.</summary>
public sealed record WhatsAppStatusResult(bool Reachable, bool Connected, string? RawJson, string? Error);

public sealed record WhatsAppProxyResult(bool Reachable, string? RawJson, string? Error);

/// <summary>
/// HTTP client in apps/api that calls the apps/whatsapp sidecar. apps/api renders the final
/// message text and posts it to <c>/send-message</c> with the internal API key. apps/api never
/// stores or reads Baileys session data.
/// </summary>
public interface IWhatsAppClient
{
    Task<WhatsAppSendResult> SendMessageAsync(string phone, string message, CancellationToken ct = default);
    Task<WhatsAppStatusResult> GetStatusAsync(CancellationToken ct = default);
    Task<WhatsAppProxyResult> GetQrAsync(CancellationToken ct = default);
    Task<WhatsAppProxyResult> GetHealthAsync(CancellationToken ct = default);
    Task<WhatsAppProxyResult> LogoutAsync(CancellationToken ct = default);
}

/// <summary>Result of an image upload.</summary>
public sealed record ImageUploadResult(string Url, string PublicId);

/// <summary>Abstracts image storage (Cloudinary implementation). Secrets stay server-side.</summary>
public interface IImageStorageProvider
{
    Task<ImageUploadResult> UploadAsync(Stream content, string fileName, string folder, CancellationToken ct = default);
}

/// <summary>Outcome of initiating a payment.</summary>
public sealed record PaymentInitiationResult(
    PaymentStatus Status,
    string? ProviderName,
    string? ProviderReference,
    string? RedirectUrl,
    string? RawResponse);

/// <summary>Outcome of handling a provider callback/webhook.</summary>
public sealed record PaymentCallbackResult(
    string? ProviderReference,
    PaymentStatus Status,
    decimal? CommissionAmount,
    string? RawResponse);

/// <summary>Payment provider abstraction. COD is active; gateways are prepared but inactive.</summary>
public interface IPaymentProvider
{
    PaymentMethod Method { get; }
    string ProviderName { get; }
    bool IsActive { get; }
    Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, CancellationToken ct = default);
    Task<PaymentCallbackResult> HandleCallbackAsync(string rawPayload, IDictionary<string, string> headers, CancellationToken ct = default);
    Task<PaymentStatus> GetPaymentStatusAsync(string providerReference, CancellationToken ct = default);
}

/// <summary>Resolves a payment provider by method/provider key.</summary>
public interface IPaymentProviderFactory
{
    IReadOnlyList<IPaymentProvider> All { get; }
    IPaymentProvider? ForMethod(PaymentMethod method);
    IPaymentProvider? ForProvider(string providerName);
}

/// <summary>Result of a shipping provider operation.</summary>
public sealed record ShipmentResult(string? TrackingNumber, string? ExternalStatus, string? ProviderName);

/// <summary>Future shipping-company integration abstraction. MVP uses manual tracking only.</summary>
public interface IShippingProvider
{
    string ProviderName { get; }
    bool IsActive { get; }
    Task<ShipmentResult> CreateShipmentAsync(Order order, CancellationToken ct = default);
    Task<ShipmentResult> GetShipmentStatusAsync(string trackingNumber, CancellationToken ct = default);
    Task<bool> CancelShipmentAsync(string trackingNumber, CancellationToken ct = default);
}
