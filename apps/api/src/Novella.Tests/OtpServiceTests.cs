using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Auth;
using Novella.Application.Common;
using Novella.Domain.Enums;
using Novella.Domain.Services;
using Xunit;

namespace Novella.Tests;

public class OtpServiceTests
{
    private const string Phone = "201234567890";

    private static OtpService NewService(TestDatabase db, FakeClock clock)
        => new(db.Db, TestSeed.OtpHasher, clock);

    [Fact]
    public async Task Issue_stores_hash_not_plain_code()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        var code = await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);

        var row = await db.Db.OtpCodes.SingleAsync();
        row.CodeHash.Should().NotBe(code);
        row.CodeHash.Should().NotContain(code);
        TestSeed.OtpHasher.Verify(code, row.CodeHash).Should().BeTrue();
    }

    [Fact]
    public async Task Verify_succeeds_then_marks_used_and_blocks_reuse()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        var code = await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);
        var otp = await svc.VerifyAsync(Phone, OtpPurpose.Register, code, default);
        otp.UsedAt.Should().NotBeNull();

        // Reuse should fail (no active unused code remains).
        var act = () => svc.VerifyAsync(Phone, OtpPurpose.Register, code, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpInvalid);
    }

    [Fact]
    public async Task Verify_expired_code_returns_expired()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        var code = await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);
        clock.Advance(TimeSpan.FromMinutes(OtpPolicy.ExpiryMinutes + 1));

        var act = () => svc.VerifyAsync(Phone, OtpPurpose.Register, code, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpExpired);
    }

    [Fact]
    public async Task Resend_within_cooldown_is_blocked()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);
        var act = () => svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpCooldown);
    }

    [Fact]
    public async Task Wrong_code_increments_attempts_and_locks_after_max()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);

        for (var i = 0; i < OtpPolicy.MaxAttempts; i++)
        {
            var act = () => svc.VerifyAsync(Phone, OtpPurpose.Register, "000000", default);
            await act.Should().ThrowAsync<AppException>();
        }

        var row = await db.Db.OtpCodes.SingleAsync();
        row.LockedUntil.Should().NotBeNull();

        // Further attempts are locked.
        var locked = () => svc.VerifyAsync(Phone, OtpPurpose.Register, "000000", default);
        (await locked.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpLocked);
    }

    [Fact]
    public async Task Purpose_is_scoped_separately()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var svc = NewService(db, clock);

        var registerCode = await svc.IssueAsync(Phone, Phone, OtpPurpose.Register, null, default);

        // Verifying under a different purpose must not match.
        var act = () => svc.VerifyAsync(Phone, OtpPurpose.ResetPassword, registerCode, default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpInvalid);
    }
}
