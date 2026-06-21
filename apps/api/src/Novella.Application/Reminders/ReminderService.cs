using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Reminders;

public sealed record ReminderSettingsDto(
    bool AbandonedCheckoutEnabled, int AbandonedCheckoutDelayHours,
    bool InactiveCustomerEnabled, int InactiveCustomerDelayDays);

public sealed record ReminderRunResult(int AbandonedSent, int InactiveSent, int Skipped);

/// <summary>
/// Customer reminders for registered customers only. Sends once per event/absence cycle (deduped
/// via <see cref="ReminderLog"/>). Triggered by an admin-protected job endpoint or a guarded hosted
/// service — no Redis/queues.
/// </summary>
public sealed class ReminderService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly WhatsAppMessenger _whatsApp;

    public ReminderService(IAppDbContext db, IClock clock, WhatsAppMessenger whatsApp)
    {
        _db = db;
        _clock = clock;
        _whatsApp = whatsApp;
    }

    public async Task<ReminderSettingsDto> GetSettingsAsync(CancellationToken ct)
    {
        var s = await _db.ReminderSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null
            ? new ReminderSettingsDto(false, 4, false, 30)
            : new ReminderSettingsDto(s.AbandonedCheckoutEnabled, s.AbandonedCheckoutDelayHours, s.InactiveCustomerEnabled, s.InactiveCustomerDelayDays);
    }

    public async Task<ReminderSettingsDto> UpdateSettingsAsync(ReminderSettingsDto req, CancellationToken ct)
    {
        var s = await _db.ReminderSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new ReminderSettings { Id = Guid.NewGuid() };
            _db.ReminderSettings.Add(s);
        }
        s.AbandonedCheckoutEnabled = req.AbandonedCheckoutEnabled;
        s.AbandonedCheckoutDelayHours = req.AbandonedCheckoutDelayHours;
        s.InactiveCustomerEnabled = req.InactiveCustomerEnabled;
        s.InactiveCustomerDelayDays = req.InactiveCustomerDelayDays;
        s.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return req;
    }

    /// <summary>Runs both reminder jobs honoring settings and dedupe rules.</summary>
    public async Task<ReminderRunResult> RunAsync(CancellationToken ct)
    {
        var settings = await _db.ReminderSettings.FirstOrDefaultAsync(ct);
        if (settings is null) return new ReminderRunResult(0, 0, 0);

        var now = _clock.UtcNow;
        var abandoned = 0; var inactive = 0; var skipped = 0;

        if (settings.AbandonedCheckoutEnabled)
        {
            var cutoff = now.AddHours(-settings.AbandonedCheckoutDelayHours);
            var carts = await _db.Carts.Include(c => c.Items).Include(c => c.Customer)
                .Where(c => c.Items.Count > 0 && (c.UpdatedAt ?? c.CreatedAt) <= cutoff)
                .ToListAsync(ct);

            foreach (var cart in carts)
            {
                if (cart.Customer is null || !cart.Customer.IsActive) { skipped++; continue; }
                var alreadyReminded = await _db.ReminderLogs.AnyAsync(r => r.ReminderType == ReminderType.AbandonedCheckout
                    && r.RelatedCartId == cart.Id && r.Status == ReminderStatus.Sent, ct);
                if (alreadyReminded) { skipped++; continue; }

                var waSettings = await _whatsApp.GetSettingsAsync(ct);
                var template = waSettings.AbandonedCheckoutTemplate ?? DefaultTemplates.AbandonedCheckout;
                var body = TemplateRenderer.Render(template, new Dictionary<string, string>
                {
                    ["name"] = cart.Customer.FullName,
                    ["link"] = "https://novellaaccessories.store/cart"
                });
                var (logId, sent, _) = await _whatsApp.SendAsync(WhatsAppMessageType.AbandonedCheckout, "abandoned_checkout", cart.Customer.PhoneNumber, cart.CustomerId, body, ct);
                LogReminder(cart.CustomerId, ReminderType.AbandonedCheckout, cart.Id, null, sent, logId);
                if (sent) abandoned++; else skipped++;
            }
        }

        if (settings.InactiveCustomerEnabled)
        {
            var cutoff = now.AddDays(-settings.InactiveCustomerDelayDays);
            var customers = await _db.Customers
                .Where(c => c.IsActive && c.IsPhoneVerified && c.LastVisitAt != null && c.LastVisitAt <= cutoff)
                .ToListAsync(ct);

            foreach (var customer in customers)
            {
                // Dedupe per absence cycle: skip if reminded since the last visit.
                var remindedSinceVisit = await _db.ReminderLogs.AnyAsync(r => r.CustomerId == customer.Id
                    && r.ReminderType == ReminderType.InactiveCustomer && r.Status == ReminderStatus.Sent
                    && r.CreatedAt >= customer.LastVisitAt, ct);
                if (remindedSinceVisit) { skipped++; continue; }

                var waSettings = await _whatsApp.GetSettingsAsync(ct);
                var template = waSettings.InactiveCustomerTemplate ?? DefaultTemplates.InactiveCustomer;
                var body = TemplateRenderer.Render(template, new Dictionary<string, string>
                {
                    ["name"] = customer.FullName,
                    ["store_link"] = "https://novellaaccessories.store"
                });
                var (logId, sent, _) = await _whatsApp.SendAsync(WhatsAppMessageType.InactiveCustomer, "inactive_customer", customer.PhoneNumber, customer.Id, body, ct);
                LogReminder(customer.Id, ReminderType.InactiveCustomer, null, null, sent, logId);
                if (sent) inactive++; else skipped++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new ReminderRunResult(abandoned, inactive, skipped);
    }

    private void LogReminder(Guid customerId, ReminderType type, Guid? cartId, Guid? sessionId, bool sent, Guid? logId)
        => _db.ReminderLogs.Add(new ReminderLog
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ReminderType = type,
            RelatedCartId = cartId,
            RelatedVisitSessionId = sessionId,
            Status = sent ? ReminderStatus.Sent : ReminderStatus.Failed,
            WhatsAppMessageLogId = logId,
            SentAt = sent ? _clock.UtcNow : null,
            CreatedAt = _clock.UtcNow
        });
}
