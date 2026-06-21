using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.WhatsApp;

// Settings DTO NEVER includes the internal API key (it is not stored in the DB).
public sealed record WhatsAppSettingsDto(
    bool IsEnabled, string TransportName, string? ServiceBaseUrl,
    string? OtpTemplate, string? OrderConfirmationTemplate, string? TwoOrderCouponTemplate,
    string? AbandonedCheckoutTemplate, string? InactiveCustomerTemplate,
    bool ServiceConfigured);

public sealed record WhatsAppSettingsUpdateRequest(
    bool IsEnabled, string? ServiceBaseUrl,
    string? OtpTemplate, string? OrderConfirmationTemplate, string? TwoOrderCouponTemplate,
    string? AbandonedCheckoutTemplate, string? InactiveCustomerTemplate);

public sealed record WhatsAppStatusDto(bool Reachable, bool Connected, bool KeyConfigured, string? Detail);

public sealed record WhatsAppMessageLogDto(
    Guid Id, Guid? CustomerId, string PhoneNumber, WhatsAppMessageType MessageType, string? TemplateKey,
    string? MessageBody, WhatsAppMessageStatus Status, string? FailureReason, int RetryCount, DateTime? SentAt, DateTime CreatedAt);

public sealed record WhatsAppTestRequest(string Phone, string? Message);

public sealed class WhatsAppMessageQuery : PageQuery
{
    public WhatsAppMessageStatus? Status { get; set; }
    public WhatsAppMessageType? Type { get; set; }
}

/// <summary>
/// Admin WhatsApp surface: settings (no raw secrets), connection status (proxied from the sidecar),
/// message logs, retry, and a test send. Whether the internal key/base URL are configured is
/// surfaced as a boolean only — never the values.
/// </summary>
public sealed class WhatsAppAdminService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IWhatsAppClient _client;
    private readonly WhatsAppMessenger _messenger;
    private readonly bool _serviceConfigured;

    public WhatsAppAdminService(IAppDbContext db, IClock clock, IWhatsAppClient client, WhatsAppMessenger messenger, IWhatsAppConfigStatus configStatus)
    {
        _db = db;
        _clock = clock;
        _client = client;
        _messenger = messenger;
        _serviceConfigured = configStatus.IsConfigured;
    }

    public async Task<WhatsAppSettingsDto> GetSettingsAsync(CancellationToken ct)
    {
        var s = await _messenger.GetSettingsAsync(ct);
        return new WhatsAppSettingsDto(s.IsEnabled, s.TransportName, s.ServiceBaseUrl,
            s.OtpTemplate, s.OrderConfirmationTemplate, s.TwoOrderCouponTemplate,
            s.AbandonedCheckoutTemplate, s.InactiveCustomerTemplate, _serviceConfigured);
    }

    public async Task<WhatsAppSettingsDto> UpdateSettingsAsync(WhatsAppSettingsUpdateRequest req, CancellationToken ct)
    {
        var s = await _db.WhatsAppSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new WhatsAppSettings { Id = Guid.NewGuid(), TransportName = "BaileysWhatsAppWeb" };
            _db.WhatsAppSettings.Add(s);
        }
        s.IsEnabled = req.IsEnabled;
        s.ServiceBaseUrl = req.ServiceBaseUrl;
        s.OtpTemplate = req.OtpTemplate;
        s.OrderConfirmationTemplate = req.OrderConfirmationTemplate;
        s.TwoOrderCouponTemplate = req.TwoOrderCouponTemplate;
        s.AbandonedCheckoutTemplate = req.AbandonedCheckoutTemplate;
        s.InactiveCustomerTemplate = req.InactiveCustomerTemplate;
        s.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetSettingsAsync(ct);
    }

    public async Task<WhatsAppStatusDto> GetStatusAsync(CancellationToken ct)
    {
        var status = await _client.GetStatusAsync(ct);
        return new WhatsAppStatusDto(status.Reachable, status.Connected, _serviceConfigured, status.Error ?? (status.Connected ? "connected" : "not_connected"));
    }

    public async Task<PagedResult<WhatsAppMessageLogDto>> GetMessagesAsync(WhatsAppMessageQuery query, CancellationToken ct)
    {
        var q = _db.WhatsAppMessageLogs.AsNoTracking().AsQueryable();
        if (query.Status is { } st) q = q.Where(m => m.Status == st);
        if (query.Type is { } t) q = q.Where(m => m.MessageType == t);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(m => new WhatsAppMessageLogDto(m.Id, m.CustomerId, m.PhoneNumber, m.MessageType, m.TemplateKey,
                m.MessageType == WhatsAppMessageType.Otp ? null : m.MessageBody, m.Status, m.FailureReason, m.RetryCount, m.SentAt, m.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<WhatsAppMessageLogDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<WhatsAppMessageLogDto> RetryAsync(Guid id, CancellationToken ct)
    {
        var log = await _messenger.RetryAsync(id, ct);
        return new WhatsAppMessageLogDto(log.Id, log.CustomerId, log.PhoneNumber, log.MessageType, log.TemplateKey,
            log.MessageType == WhatsAppMessageType.Otp ? null : log.MessageBody, log.Status, log.FailureReason, log.RetryCount, log.SentAt, log.CreatedAt);
    }

    public async Task<WhatsAppMessageLogDto> SendTestAsync(WhatsAppTestRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Phone))
            throw AppException.Validation("Phone is required.");
        var body = TemplateRenderer.Render(DefaultTemplates.AdminTest, new Dictionary<string, string>
        {
            ["message"] = string.IsNullOrWhiteSpace(req.Message) ? "Hello from Novella Accessories." : req.Message
        });
        var (logId, _, _) = await _messenger.SendAsync(WhatsAppMessageType.AdminTest, "admin_test", req.Phone, null, body, ct);
        var log = await _db.WhatsAppMessageLogs.AsNoTracking().FirstAsync(m => m.Id == logId, ct);
        return new WhatsAppMessageLogDto(log.Id, log.CustomerId, log.PhoneNumber, log.MessageType, log.TemplateKey,
            log.MessageBody, log.Status, log.FailureReason, log.RetryCount, log.SentAt, log.CreatedAt);
    }
}

/// <summary>Reports whether WhatsApp base URL + internal key are configured (boolean only).</summary>
public interface IWhatsAppConfigStatus
{
    bool IsConfigured { get; }
}
