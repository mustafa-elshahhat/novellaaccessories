using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>
/// WhatsApp business configuration + templates only. Holds NO Baileys session/auth state —
/// that lives in apps/whatsapp's MongoDB. The internal API key is never stored here.
/// </summary>
public class WhatsAppSettings
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public string TransportName { get; set; } = "BaileysWhatsAppWeb";
    public string? TwoOrderCouponTemplate { get; set; }
    public string? AbandonedCheckoutTemplate { get; set; }
    public string? InactiveCustomerTemplate { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Business message delivery log (stored in SQL Server, never the sidecar).</summary>
public class WhatsAppMessageLog
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public WhatsAppMessageType MessageType { get; set; }
    public string? TemplateKey { get; set; }
    public string? MessageBody { get; set; }
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Pending;
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Singleton reminder configuration.</summary>
public class ReminderSettings
{
    public Guid Id { get; set; }
    public bool AbandonedCheckoutEnabled { get; set; }
    public int AbandonedCheckoutDelayHours { get; set; } = 4;
    public bool InactiveCustomerEnabled { get; set; }
    public int InactiveCustomerDelayDays { get; set; } = 30;
    public DateTime UpdatedAt { get; set; }
}

/// <summary>De-dupe + audit log for reminder attempts (once per event/absence cycle).</summary>
public class ReminderLog
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public ReminderType ReminderType { get; set; }
    public Guid? RelatedCartId { get; set; }
    public Guid? RelatedVisitSessionId { get; set; }
    public ReminderStatus Status { get; set; }
    public Guid? WhatsAppMessageLogId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
