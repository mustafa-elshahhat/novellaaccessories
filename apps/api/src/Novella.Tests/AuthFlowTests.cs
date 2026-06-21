using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Auth;
using Novella.Application.Common;
using Novella.Application.WhatsApp;
using Novella.Domain.Enums;
using Xunit;

namespace Novella.Tests;

/// <summary>Deterministic OTP hasher so tests know the issued code.</summary>
file sealed class FixedOtpHasher : IOtpHasher
{
    public const string Code = "123456";
    public string GenerateCode() => Code;
    public string Hash(string code) => "hash:" + code;
    public bool Verify(string code, string hash) => hash == "hash:" + code;
}

file sealed class FakeJwt : IJwtTokenService
{
    public string CreateCustomerToken(Guid customerId, string phone, string fullName) => "customer-token";
    public string CreateAdminToken(Guid adminId, string username, string displayName) => "admin-token";
}

public class AuthFlowTests
{
    private static AuthService Build(TestDatabase db, FakeClock clock, out FakeWhatsAppClient wa)
    {
        wa = new FakeWhatsAppClient();
        TestSeed.EnableWhatsApp(db.Db, clock);
        var messenger = new WhatsAppMessenger(db.Db, wa, clock);
        var otp = new OtpService(db.Db, new FixedOtpHasher(), clock);
        return new AuthService(db.Db, TestSeed.Passwords, new FakeJwt(), clock, otp, messenger);
    }

    [Fact]
    public async Task Register_then_verify_then_login_succeeds()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var auth = Build(db, clock, out _);

        await auth.RegisterAsync(new RegisterRequest("Sara", "01000000001", "Password1"), default);
        var customer = await db.Db.Customers.FirstAsync();
        customer.IsPhoneVerified.Should().BeFalse();

        var token = await auth.VerifyPhoneAsync(new VerifyPhoneRequest("01000000001", FixedOtpHasher.Code), default);
        token.Token.Should().Be("customer-token");
        (await db.Db.Customers.FirstAsync()).IsPhoneVerified.Should().BeTrue();

        var login = await auth.LoginAsync(new LoginRequest("01000000001", "Password1"), default);
        login.Customer.IsPhoneVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Login_unverified_phone_is_rejected()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var auth = Build(db, clock, out _);

        await auth.RegisterAsync(new RegisterRequest("Sara", "01000000002", "Password1"), default);
        var act = () => auth.LoginAsync(new LoginRequest("01000000002", "Password1"), default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.PhoneNotVerified);
    }

    [Fact]
    public async Task Change_phone_sends_otp_to_new_number_and_only_saves_after_verify()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var auth = Build(db, clock, out var wa);

        var customer = TestSeed.AddCustomer(db.Db, clock, phone: "201000000003");

        await auth.RequestPhoneChangeAsync(customer.Id, new ChangePhoneRequest("01055555555"), default);

        // OTP must be issued for the NEW normalized number under the ChangePhone purpose.
        var otpRow = await db.Db.OtpCodes.FirstAsync(o => o.Purpose == OtpPurpose.ChangePhone);
        otpRow.PhoneNumberNormalized.Should().Be("201055555555");
        wa.Sends.Should().ContainSingle(s => s.Phone == "01055555555");

        // Phone not changed yet.
        (await db.Db.Customers.FirstAsync(c => c.Id == customer.Id)).PhoneNumberNormalized.Should().Be("201000000003");

        await auth.VerifyPhoneChangeAsync(customer.Id, new ChangePhoneVerifyRequest("01055555555", FixedOtpHasher.Code), default);

        var updated = await db.Db.Customers.FirstAsync(c => c.Id == customer.Id);
        updated.PhoneNumberNormalized.Should().Be("201055555555");
        (await db.Db.CustomerPhoneChangeRequests.FirstAsync()).Status.Should().Be(PhoneChangeStatus.Verified);
    }

    [Fact]
    public async Task Register_duplicate_verified_phone_is_rejected()
    {
        using var db = new TestDatabase();
        var clock = new FakeClock();
        var auth = Build(db, clock, out _);
        TestSeed.AddCustomer(db.Db, clock, phone: "201000000009", verified: true);

        var act = () => auth.RegisterAsync(new RegisterRequest("Dup", "01000000009", "Password1"), default);
        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.PhoneAlreadyUsed);
    }
}
