using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Analytics;
using Novella.Application.Common;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task Track_events_strips_pii_metadata_before_storage()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var service = new AnalyticsService(db.Db, clock);
        var session = await service.StartSessionAsync(new StartSessionRequest("anon-1", "/", null, null, null, null, "mobile", "en"), null, default);

        await service.TrackEventsAsync(new TrackEventsRequest(new[]
        {
            new TrackEventRequest(session.SessionId, session.VisitorId, AnalyticsEventType.PageView, "/", null, null,
                "{\"component\":\"hero\",\"phone\":\"201000000001\",\"email\":\"x@example.com\"}")
        }), null, default);

        var stored = await db.Db.AnalyticsEvents.SingleAsync();
        stored.MetadataJson.Should().Contain("component");
        stored.MetadataJson.Should().NotContain("phone");
        stored.MetadataJson.Should().NotContain("email");
        stored.MetadataJson.Should().NotContain("201000000001");
        stored.MetadataJson.Should().NotContain("x@example.com");
    }

    [Fact]
    public async Task Track_events_rejects_oversized_metadata_and_batches()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var service = new AnalyticsService(db.Db, clock);
        var session = await service.StartSessionAsync(new StartSessionRequest("anon-2", "/", null, null, null, null, null, "en"), null, default);

        var tooLarge = "{\"value\":\"" + new string('x', 2050) + "\"}";
        var oversized = () => service.TrackEventsAsync(new TrackEventsRequest(new[]
        {
            new TrackEventRequest(session.SessionId, session.VisitorId, AnalyticsEventType.PageView, "/", null, null, tooLarge)
        }), null, default);

        await oversized.Should().ThrowAsync<AppException>();

        var many = Enumerable.Range(0, 26)
            .Select(_ => new TrackEventRequest(session.SessionId, session.VisitorId, AnalyticsEventType.PageView, "/", null, null, null))
            .ToArray();
        var tooMany = () => service.TrackEventsAsync(new TrackEventsRequest(many), null, default);

        await tooMany.Should().ThrowAsync<AppException>();
    }
}
