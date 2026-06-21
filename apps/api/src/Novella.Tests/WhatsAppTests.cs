using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.WhatsApp;
using Novella.Domain.Enums;
using Novella.Domain.Services;
using Novella.Infrastructure.WhatsApp;
using Xunit;

namespace Novella.Tests;

public class WhatsAppTests
{
    [Fact]
    public void Client_uses_send_message_endpoint_not_send()
    {
        // Contract guard: apps/api must call /send-message, never the removed /send.
        WhatsAppClient.SendMessagePath.Should().Be("/send-message");
        WhatsAppClient.SendMessagePath.Should().NotBe("/send");
        WhatsAppClient.InternalApiKeyHeader.Should().Be("x-internal-api-key");
    }

    [Fact]
    public async Task Send_writes_sent_log_on_success()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        TestSeed.EnableWhatsApp(db.Db, clock);
        var client = new FakeWhatsAppClient { ShouldSucceed = true };
        var messenger = new WhatsAppMessenger(db.Db, client, clock);

        var (logId, sent, _) = await messenger.SendAsync(WhatsAppMessageType.Otp, "otp", "201000000001", null, "code 123456", default);

        sent.Should().BeTrue();
        client.Sends.Should().HaveCount(1);
        var log = await db.Db.WhatsAppMessageLogs.FirstAsync(l => l.Id == logId);
        log.Status.Should().Be(WhatsAppMessageStatus.Sent);
        log.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Send_writes_failed_log_on_failure()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        TestSeed.EnableWhatsApp(db.Db, clock);
        var client = new FakeWhatsAppClient { ShouldSucceed = false };
        var messenger = new WhatsAppMessenger(db.Db, client, clock);

        var (logId, sent, error) = await messenger.SendAsync(WhatsAppMessageType.Otp, "otp", "201000000001", null, "body", default);

        sent.Should().BeFalse();
        error.Should().Be("send_failed");
        var log = await db.Db.WhatsAppMessageLogs.FirstAsync(l => l.Id == logId);
        log.Status.Should().Be(WhatsAppMessageStatus.Failed);
        log.FailureReason.Should().Be("send_failed");
    }

    [Fact]
    public async Task Disabled_whatsapp_suppresses_send_but_logs()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        TestSeed.EnableWhatsApp(db.Db, clock, enabled: false);
        var client = new FakeWhatsAppClient { ShouldSucceed = true };
        var messenger = new WhatsAppMessenger(db.Db, client, clock);

        var (logId, sent, error) = await messenger.SendAsync(WhatsAppMessageType.Otp, "otp", "201000000001", null, "body", default);

        sent.Should().BeFalse();
        error.Should().Be("whatsapp_disabled");
        client.Sends.Should().BeEmpty();
        (await db.Db.WhatsAppMessageLogs.FirstAsync(l => l.Id == logId)).Status.Should().Be(WhatsAppMessageStatus.Failed);
    }

    [Fact]
    public async Task Retry_increments_count_and_can_succeed()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        TestSeed.EnableWhatsApp(db.Db, clock);
        var client = new FakeWhatsAppClient { ShouldSucceed = false };
        var messenger = new WhatsAppMessenger(db.Db, client, clock);

        var (logId, _, _) = await messenger.SendAsync(WhatsAppMessageType.OrderConfirmation, "order", "201000000001", null, "body", default);

        client.ShouldSucceed = true;
        var log = await messenger.RetryAsync(logId, default);

        log.RetryCount.Should().Be(1);
        log.Status.Should().Be(WhatsAppMessageStatus.Sent);
    }

    [Fact]
    public async Task Retry_is_bounded()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        TestSeed.EnableWhatsApp(db.Db, clock);
        var client = new FakeWhatsAppClient { ShouldSucceed = false };
        var messenger = new WhatsAppMessenger(db.Db, client, clock);

        var (logId, _, _) = await messenger.SendAsync(WhatsAppMessageType.OrderConfirmation, "order", "201000000001", null, "body", default);

        for (var i = 0; i < WhatsAppPolicy.MaxRetries; i++)
            await messenger.RetryAsync(logId, default);

        var act = () => messenger.RetryAsync(logId, default);
        await act.Should().ThrowAsync<Application.Common.AppException>();
    }
}
