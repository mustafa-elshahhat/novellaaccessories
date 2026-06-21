using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.WhatsApp;

/// <summary>Renders <c>{{token}}</c> placeholders in a template against a value map.</summary>
public static class TemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{{" + key + "}}", value);
        return result;
    }
}

/// <summary>Built-in default message templates used when an admin template is not configured.</summary>
public static class DefaultTemplates
{
    public const string Otp = "Novella Accessories: your verification code is {{code}}. It expires in {{minutes}} minutes. Do not share it.";
    public const string OrderConfirmation = "Hi {{name}}, your order {{order_number}} is confirmed. Total: {{total}} EGP, paid via {{payment_method}}. Thank you for shopping with Novella.";
    public const string TwoOrderCoupon = "Thank you {{name}}! Here is your reward coupon {{coupon_code}} for {{discount}}% off, valid until {{expiry_date}}.";
    public const string AbandonedCheckout = "Hi {{name}}, you left items in your cart. Complete your order here: {{link}}";
    public const string InactiveCustomer = "Hi {{name}}, we miss you at Novella Accessories. Discover what's new: {{store_link}}";
    public const string AdminTest = "Novella Accessories test message: {{message}}";
}

/// <summary>
/// Application-level WhatsApp orchestration. apps/api renders the final text, writes a
/// <see cref="WhatsAppMessageLog"/> for every send, respects <see cref="WhatsAppSettings.IsEnabled"/>,
/// and calls the sidecar via <see cref="IWhatsAppClient"/> (POST /send-message). Bounded retries.
/// </summary>
public sealed class WhatsAppMessenger
{
    private readonly IAppDbContext _db;
    private readonly IWhatsAppClient _client;
    private readonly IClock _clock;

    public WhatsAppMessenger(IAppDbContext db, IWhatsAppClient client, IClock clock)
    {
        _db = db;
        _client = client;
        _clock = clock;
    }

    public async Task<WhatsAppSettings> GetSettingsAsync(CancellationToken ct)
        => await _db.WhatsAppSettings.FirstOrDefaultAsync(ct)
           ?? new WhatsAppSettings { Id = Guid.NewGuid(), IsEnabled = false, UpdatedAt = _clock.UtcNow };

    /// <summary>
    /// Logs and attempts a send. When WhatsApp is disabled the send is suppressed safely and the
    /// log records why. Returns the created log id and the send result.
    /// </summary>
    public async Task<(Guid logId, bool sent, string? error)> SendAsync(
        WhatsAppMessageType type, string? templateKey, string phone, Guid? customerId, string body, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var log = new WhatsAppMessageLog
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PhoneNumber = phone,
            MessageType = type,
            TemplateKey = templateKey,
            MessageBody = body,
            Status = WhatsAppMessageStatus.Pending,
            RetryCount = 0,
            CreatedAt = now
        };
        _db.WhatsAppMessageLogs.Add(log);

        var settings = await _db.WhatsAppSettings.FirstOrDefaultAsync(ct);
        if (settings is null || !settings.IsEnabled)
        {
            log.Status = WhatsAppMessageStatus.Failed;
            log.FailureReason = "whatsapp_disabled";
            await _db.SaveChangesAsync(ct);
            return (log.Id, false, "whatsapp_disabled");
        }

        var result = await _client.SendMessageAsync(phone, body, ct);
        if (result.Success)
        {
            log.Status = WhatsAppMessageStatus.Sent;
            log.SentAt = _clock.UtcNow;
        }
        else
        {
            log.Status = WhatsAppMessageStatus.Failed;
            log.FailureReason = result.Error;
        }
        await _db.SaveChangesAsync(ct);
        return (log.Id, result.Success, result.Error);
    }

    /// <summary>Admin-triggered retry of a failed message. Bounded by <see cref="WhatsAppPolicy.MaxRetries"/>.</summary>
    public async Task<WhatsAppMessageLog> RetryAsync(Guid logId, CancellationToken ct)
    {
        var log = await _db.WhatsAppMessageLogs.FirstOrDefaultAsync(x => x.Id == logId, ct)
            ?? throw AppException.NotFound("Message log not found.");

        if (log.Status == WhatsAppMessageStatus.Sent)
            throw AppException.Conflict("Message already sent.");

        if (log.RetryCount >= WhatsAppPolicy.MaxRetries)
            throw new AppException(ErrorCodes.WhatsAppRetryLimit, "Retry limit reached for this message.", 409);

        var settings = await _db.WhatsAppSettings.FirstOrDefaultAsync(ct);
        log.RetryCount += 1;

        if (settings is null || !settings.IsEnabled)
        {
            log.Status = WhatsAppMessageStatus.Failed;
            log.FailureReason = "whatsapp_disabled";
            await _db.SaveChangesAsync(ct);
            return log;
        }

        var result = await _client.SendMessageAsync(log.PhoneNumber, log.MessageBody ?? string.Empty, ct);
        if (result.Success)
        {
            log.Status = WhatsAppMessageStatus.Sent;
            log.SentAt = _clock.UtcNow;
            log.FailureReason = null;
        }
        else
        {
            log.Status = WhatsAppMessageStatus.Failed;
            log.FailureReason = result.Error;
        }
        await _db.SaveChangesAsync(ct);
        return log;
    }
}
