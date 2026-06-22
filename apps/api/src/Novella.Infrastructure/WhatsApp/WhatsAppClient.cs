using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Infrastructure.Configuration;

namespace Novella.Infrastructure.WhatsApp;

/// <summary>
/// HTTP client that calls the apps/whatsapp sidecar. Uses the primary <c>POST /send-message</c>
/// endpoint (NOT the removed <c>/send</c>) and authenticates with the internal API key via the
/// <c>x-internal-api-key</c> header. Never sends/stores Baileys session data.
/// </summary>
public sealed class WhatsAppClient : IWhatsAppClient
{
    public const string SendMessagePath = "/send-message";
    public const string StatusPath = "/status";
    public const string InternalApiKeyHeader = "x-internal-api-key";

    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppClient> _logger;

    public WhatsAppClient(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WhatsAppSendResult> SendMessageAsync(string phone, string message, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return new WhatsAppSendResult(false, null, "whatsapp_not_configured", false);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(SendMessagePath))
            {
                Content = JsonContent.Create(new { phone, message })
            };
            request.Headers.TryAddWithoutValidation(InternalApiKeyHeader, _options.InternalApiKey);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                // Phone/message are intentionally NOT logged.
                _logger.LogInformation("WhatsApp send succeeded ({Status})", (int)response.StatusCode);
                var (_, providerId) = TryReadSuccess(body);
                return new WhatsAppSendResult(true, providerId, null, false);
            }

            var (error, retryable) = TryReadError(body, response.StatusCode);
            _logger.LogWarning("WhatsApp send failed ({Status}): {Error}", (int)response.StatusCode, error);
            return new WhatsAppSendResult(false, null, error, retryable);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp send transport error");
            return new WhatsAppSendResult(false, null, "transport_error", true);
        }
    }

    public async Task<WhatsAppStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return new WhatsAppStatusResult(false, false, null, "whatsapp_not_configured");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(StatusPath));
            request.Headers.TryAddWithoutValidation(InternalApiKeyHeader, _options.InternalApiKey);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new WhatsAppStatusResult(true, false, null, $"status_{(int)response.StatusCode}");

            var connected = false;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("connected", out var c) && c.ValueKind == JsonValueKind.True)
                    connected = true;
                else if (doc.RootElement.TryGetProperty("state", out var state) &&
                         string.Equals(state.GetString(), "connected", StringComparison.OrdinalIgnoreCase))
                    connected = true;
                else if (doc.RootElement.TryGetProperty("status", out var s) &&
                         string.Equals(s.GetString(), "connected", StringComparison.OrdinalIgnoreCase))
                    connected = true;
            }
            catch (JsonException) { /* keep raw body */ }

            return new WhatsAppStatusResult(true, connected, body, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp status transport error");
            return new WhatsAppStatusResult(false, false, null, "transport_error");
        }
    }

    private Uri BuildUri(string path) => new(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static (bool success, string? providerId) TryReadSuccess(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var providerId = root.TryGetProperty("messageId", out var m) ? m.GetString()
                : root.TryGetProperty("id", out var id) ? id.GetString() : null;
            return (true, providerId);
        }
        catch (JsonException) { return (true, null); }
    }

    private static (string error, bool retryable) TryReadError(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var error = root.TryGetProperty("error", out var e) ? e.GetString() ?? "send_failed" : "send_failed";
            var retryable = root.TryGetProperty("retryable", out var r)
                ? r.ValueKind == JsonValueKind.True
                : (int)status >= 500;
            return (error, retryable);
        }
        catch (JsonException)
        {
            return ("send_failed", (int)status >= 500);
        }
    }
}
