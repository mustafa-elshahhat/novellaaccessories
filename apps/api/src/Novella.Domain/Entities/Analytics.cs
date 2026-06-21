using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>An anonymous (later identified) visitor.</summary>
public class AnalyticsVisitor
{
    public Guid Id { get; set; }
    public string AnonymousId { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}

/// <summary>A visit session with acquisition context.</summary>
public class AnalyticsSession
{
    public Guid Id { get; set; }
    public Guid VisitorId { get; set; }
    public AnalyticsVisitor? Visitor { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string? LandingPage { get; set; }
    public string? Referrer { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? DeviceType { get; set; }
    public string? Language { get; set; }
    public Guid? ConvertedOrderId { get; set; }
}

/// <summary>A single analytics event.</summary>
public class AnalyticsEvent
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public AnalyticsSession? Session { get; set; }
    public Guid VisitorId { get; set; }
    public Guid? CustomerId { get; set; }
    public AnalyticsEventType EventType { get; set; }
    public string? PageUrl { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? OrderId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
