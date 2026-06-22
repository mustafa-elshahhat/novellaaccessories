using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;

namespace Novella.Application.Analytics;

public sealed record StartSessionRequest(
    string AnonymousId, string? LandingPage, string? Referrer,
    string? UtmSource, string? UtmMedium, string? UtmCampaign, string? DeviceType, string? Language);

public sealed record StartSessionResponse(Guid VisitorId, Guid SessionId);

public sealed record TrackEventRequest(
    Guid SessionId, Guid VisitorId, AnalyticsEventType EventType, string? PageUrl, Guid? ProductId, Guid? OrderId, string? MetadataJson);

public sealed record TrackEventsRequest(IReadOnlyList<TrackEventRequest> Events);

public sealed record IdentifyRequest(Guid SessionId, Guid VisitorId);

/// <summary>Lightweight first-party analytics ingestion (visitor, session, events, identify).</summary>
public sealed class AnalyticsService
{
    private const int MaxEventsPerRequest = 25;
    private const int MaxMetadataLength = 2048;
    private const int MaxMetadataProperties = 20;
    private static readonly HashSet<string> BlockedMetadataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "phone", "phoneNumber", "mobile", "email", "name", "fullName", "firstName", "lastName",
        "address", "detailedAddress", "password", "token", "jwt", "secret", "otp", "code"
    };

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AnalyticsService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<StartSessionResponse> StartSessionAsync(StartSessionRequest req, Guid? customerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.AnonymousId))
            throw AppException.Validation("anonymousId is required.");
        if (req.AnonymousId.Length > 128)
            throw AppException.Validation("anonymousId is too long.");

        var now = _clock.UtcNow;
        var visitor = await _db.AnalyticsVisitors.FirstOrDefaultAsync(v => v.AnonymousId == req.AnonymousId, ct);
        if (visitor is null)
        {
            visitor = new AnalyticsVisitor { Id = Guid.NewGuid(), AnonymousId = req.AnonymousId, FirstSeenAt = now, LastSeenAt = now, CustomerId = customerId };
            _db.AnalyticsVisitors.Add(visitor);
        }
        else
        {
            visitor.LastSeenAt = now;
            if (customerId is not null) visitor.CustomerId = customerId;
        }

        var session = new AnalyticsSession
        {
            Id = Guid.NewGuid(),
            VisitorId = visitor.Id,
            CustomerId = customerId,
            StartedAt = now,
            LastActivityAt = now,
            LandingPage = req.LandingPage,
            Referrer = req.Referrer,
            UtmSource = req.UtmSource,
            UtmMedium = req.UtmMedium,
            UtmCampaign = req.UtmCampaign,
            DeviceType = req.DeviceType,
            Language = req.Language
        };
        _db.AnalyticsSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return new StartSessionResponse(visitor.Id, session.Id);
    }

    public async Task TrackEventsAsync(TrackEventsRequest req, Guid? customerId, CancellationToken ct)
    {
        if (req.Events.Count == 0) return;
        if (req.Events.Count > MaxEventsPerRequest)
            throw AppException.Validation($"At most {MaxEventsPerRequest} analytics events can be sent at once.");
        var now = _clock.UtcNow;

        foreach (var e in req.Events)
        {
            if (e.PageUrl is { Length: > 1024 })
                throw AppException.Validation("pageUrl is too long.");

            _db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                SessionId = e.SessionId,
                VisitorId = e.VisitorId,
                CustomerId = customerId,
                EventType = e.EventType,
                PageUrl = e.PageUrl,
                ProductId = e.ProductId,
                OrderId = e.OrderId,
                MetadataJson = SanitizeMetadata(e.MetadataJson),
                CreatedAt = now
            });
        }

        var sessionId = req.Events[0].SessionId;
        var session = await _db.AnalyticsSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is not null)
        {
            session.LastActivityAt = now;
            // Link an order placed in this session for conversion tracking.
            var placed = req.Events.FirstOrDefault(e => e.EventType == AnalyticsEventType.OrderPlaced && e.OrderId is not null);
            if (placed is not null) session.ConvertedOrderId = placed.OrderId;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task IdentifyAsync(IdentifyRequest req, Guid customerId, CancellationToken ct)
    {
        var session = await _db.AnalyticsSessions.FirstOrDefaultAsync(s => s.Id == req.SessionId, ct);
        if (session is not null) session.CustomerId = customerId;

        var visitor = await _db.AnalyticsVisitors.FirstOrDefaultAsync(v => v.Id == req.VisitorId, ct);
        if (visitor is not null) visitor.CustomerId = customerId;

        await _db.SaveChangesAsync(ct);
    }

    private static string? SanitizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        if (metadataJson.Length > MaxMetadataLength)
            throw AppException.Validation("Analytics metadata is too large.");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(metadataJson);
        }
        catch (JsonException)
        {
            throw AppException.Validation("Analytics metadata must be valid JSON.");
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw AppException.Validation("Analytics metadata must be a JSON object.");

            var sanitized = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (sanitized.Count >= MaxMetadataProperties) break;
                if (BlockedMetadataKeys.Contains(prop.Name)) continue;

                sanitized[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => SanitizeString(prop.Value.GetString()),
                    JsonValueKind.Number when prop.Value.TryGetDecimal(out var d) => d,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => null
                };
            }

            return sanitized.Count == 0 ? null : JsonSerializer.Serialize(sanitized);
        }
    }

    private static string? SanitizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var trimmed = value.Trim();
        if (trimmed.Contains('@') || trimmed.Any(char.IsControl))
            return null;
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }
}
