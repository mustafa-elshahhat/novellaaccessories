using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Application.WhatsApp;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.Auth;

/// <summary>Customer authentication and OTP-driven flows (register, login, reset, change phone).</summary>
public sealed class AuthService
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> PhoneWindows = new();
    private static readonly object RateLimitLock = new();
    private const int PhonePermitLimit = 30;
    private static readonly TimeSpan PhoneWindow = TimeSpan.FromMinutes(10);

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private readonly OtpService _otp;
    private readonly WhatsAppMessenger _whatsApp;

    public AuthService(IAppDbContext db, IPasswordHasher passwords, IJwtTokenService jwt, IClock clock,
        OtpService otp, WhatsAppMessenger whatsApp)
    {
        _db = db;
        _passwords = passwords;
        _jwt = jwt;
        _clock = clock;
        _otp = otp;
        _whatsApp = whatsApp;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(req.PhoneNumber);
        EnforcePhoneRateLimit($"register:{normalized}", _clock.UtcNow);
        if (normalized.Length < 8)
            throw AppException.Validation("Invalid phone number.");
        if (string.IsNullOrWhiteSpace(req.FullName))
            throw AppException.Validation("Name is required.");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            throw AppException.Validation("Password must be at least 6 characters.");

        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumberNormalized == normalized, ct);
        if (existing is { IsPhoneVerified: true })
            throw new AppException(ErrorCodes.PhoneAlreadyUsed, "Phone number is already registered.", 409);

        Customer customer;
        if (existing is not null)
        {
            // Unverified account: refresh details and re-send verification.
            existing.FullName = req.FullName;
            existing.PasswordHash = _passwords.Hash(req.Password);
            existing.PhoneNumber = req.PhoneNumber;
            existing.UpdatedAt = _clock.UtcNow;
            customer = existing;
        }
        else
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FullName = req.FullName,
                PhoneNumber = req.PhoneNumber,
                PhoneNumberNormalized = normalized,
                PasswordHash = _passwords.Hash(req.Password),
                IsPhoneVerified = false,
                IsActive = true,
                CreatedAt = _clock.UtcNow
            };
            _db.Customers.Add(customer);
        }
        await _db.SaveChangesAsync(ct);

        await IssueAndSendOtpAsync(req.PhoneNumber, normalized, OtpPurpose.Register, customer.Id, customer.FullName, ct);
        return new RegisterResponse(true, req.PhoneNumber);
    }

    public async Task<AuthTokenResponse> VerifyPhoneAsync(VerifyPhoneRequest req, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(req.PhoneNumber);
        EnforcePhoneRateLimit($"verify:{normalized}", _clock.UtcNow);
        await _otp.VerifyAsync(normalized, OtpPurpose.Register, req.Code, ct);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumberNormalized == normalized, ct)
            ?? throw AppException.NotFound("Customer not found.");

        customer.IsPhoneVerified = true;
        customer.UpdatedAt = _clock.UtcNow;
        customer.LastLoginAt = _clock.UtcNow;
        customer.LastVisitAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return BuildToken(customer);
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(req.PhoneNumber);
        EnforcePhoneRateLimit($"login:{normalized}", _clock.UtcNow);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumberNormalized == normalized, ct);

        if (customer is null || !customer.IsActive || !_passwords.Verify(req.Password, customer.PasswordHash))
            throw new AppException(ErrorCodes.AuthInvalidCredentials, "Invalid phone number or password.", 401);

        if (!customer.IsPhoneVerified)
            throw new AppException(ErrorCodes.PhoneNotVerified, "Phone number is not verified.", 403);

        customer.LastLoginAt = _clock.UtcNow;
        customer.LastVisitAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return BuildToken(customer);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(req.PhoneNumber);
        EnforcePhoneRateLimit($"reset-request:{normalized}", _clock.UtcNow);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumberNormalized == normalized, ct);
        // Do not reveal whether the account exists.
        if (customer is null || !customer.IsActive) return;

        await IssueAndSendOtpAsync(customer.PhoneNumber, normalized, OtpPurpose.ResetPassword, customer.Id, customer.FullName, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            throw AppException.Validation("Password must be at least 6 characters.");

        var normalized = PhoneNumberNormalizer.Normalize(req.PhoneNumber);
        EnforcePhoneRateLimit($"reset:{normalized}", _clock.UtcNow);
        await _otp.VerifyAsync(normalized, OtpPurpose.ResetPassword, req.Code, ct);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.PhoneNumberNormalized == normalized, ct)
            ?? throw AppException.NotFound("Customer not found.");
        if (!customer.IsActive)
            throw AppException.Forbidden("Customer account is inactive.");

        customer.PasswordHash = _passwords.Hash(req.NewPassword);
        customer.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RequestPhoneChangeAsync(Guid customerId, ChangePhoneRequest req, CancellationToken ct)
    {
        var newNormalized = PhoneNumberNormalizer.Normalize(req.NewPhoneNumber);
        EnforcePhoneRateLimit($"change-request:{newNormalized}", _clock.UtcNow);
        if (newNormalized.Length < 8)
            throw AppException.Validation("Invalid phone number.");

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw AppException.NotFound("Customer not found.");
        if (!customer.IsActive)
            throw AppException.Forbidden("Customer account is inactive.");
        if (!customer.IsPhoneVerified)
            throw new AppException(ErrorCodes.PhoneNotVerified, "Phone number is not verified.", 403);

        var taken = await _db.Customers.AnyAsync(c => c.PhoneNumberNormalized == newNormalized && c.Id != customerId, ct);
        if (taken)
            throw new AppException(ErrorCodes.PhoneAlreadyUsed, "Phone number is already registered.", 409);

        _db.CustomerPhoneChangeRequests.Add(new CustomerPhoneChangeRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OldPhoneNumber = customer.PhoneNumber,
            NewPhoneNumber = req.NewPhoneNumber,
            NewPhoneNumberNormalized = newNormalized,
            Status = PhoneChangeStatus.Pending,
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // OTP is sent to the NEW number; the phone is only saved after verification.
        await IssueAndSendOtpAsync(req.NewPhoneNumber, newNormalized, OtpPurpose.ChangePhone, customerId, customer.FullName, ct);
    }

    public async Task VerifyPhoneChangeAsync(Guid customerId, ChangePhoneVerifyRequest req, CancellationToken ct)
    {
        var newNormalized = PhoneNumberNormalizer.Normalize(req.NewPhoneNumber);
        EnforcePhoneRateLimit($"change-verify:{newNormalized}", _clock.UtcNow);
        await _otp.VerifyAsync(newNormalized, OtpPurpose.ChangePhone, req.Code, ct);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw AppException.NotFound("Customer not found.");
        if (!customer.IsActive)
            throw AppException.Forbidden("Customer account is inactive.");

        var request = await _db.CustomerPhoneChangeRequests
            .Where(r => r.CustomerId == customerId && r.NewPhoneNumberNormalized == newNormalized && r.Status == PhoneChangeStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Guard against the number being taken between request and verify.
        var taken = await _db.Customers.AnyAsync(c => c.PhoneNumberNormalized == newNormalized && c.Id != customerId, ct);
        if (taken)
            throw new AppException(ErrorCodes.PhoneAlreadyUsed, "Phone number is already registered.", 409);

        customer.PhoneNumber = req.NewPhoneNumber;
        customer.PhoneNumberNormalized = newNormalized;
        customer.UpdatedAt = _clock.UtcNow;

        if (request is not null)
        {
            request.Status = PhoneChangeStatus.Verified;
            request.VerifiedAt = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CustomerProfileDto> GetProfileAsync(Guid customerId, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw AppException.NotFound("Customer not found.");
        if (!customer.IsActive)
            throw AppException.Forbidden("Customer account is inactive.");
        return Map(customer);
    }

    private AuthTokenResponse BuildToken(Customer c)
        => new(_jwt.CreateCustomerToken(c.Id, c.PhoneNumber, c.FullName), Map(c));

    private static CustomerProfileDto Map(Customer c)
        => new(c.Id, c.FullName, c.PhoneNumber, c.IsPhoneVerified, c.CreatedAt);

    private async Task IssueAndSendOtpAsync(string phone, string normalized, OtpPurpose purpose, Guid customerId, string name, CancellationToken ct)
    {
        var code = await _otp.IssueAsync(phone, normalized, purpose, customerId, ct);
        var settings = await _whatsApp.GetSettingsAsync(ct);
        var template = settings.OtpTemplate ?? DefaultTemplates.Otp;
        var body = TemplateRenderer.Render(template, new Dictionary<string, string>
        {
            ["name"] = name,
            ["code"] = code,
            ["minutes"] = OtpPolicy.ExpiryMinutes.ToString()
        });
        await _whatsApp.SendAsync(WhatsAppMessageType.Otp, "otp", phone, customerId, body, ct);
        // The plain code is now out of scope; it was never logged or returned.
    }

    private static void EnforcePhoneRateLimit(string key, DateTime now)
    {
        lock (RateLimitLock)
        {
            var window = PhoneWindows.GetOrAdd(key, _ => new Queue<DateTime>());
            while (window.Count > 0 && now - window.Peek() > PhoneWindow)
                window.Dequeue();
            if (window.Count >= PhonePermitLimit)
                throw new AppException(ErrorCodes.RateLimited, "Too many attempts for this phone number. Please try again later.", 429);
            window.Enqueue(now);
        }
    }
}

/// <summary>Single back-office admin authentication.</summary>
public sealed class AdminAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;

    public AdminAuthService(IAppDbContext db, IPasswordHasher passwords, IJwtTokenService jwt, IClock clock)
    {
        _db = db;
        _passwords = passwords;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AdminTokenResponse> LoginAsync(AdminLoginRequest req, CancellationToken ct)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Username == req.Username, ct);
        if (admin is null || !admin.IsActive || !_passwords.Verify(req.Password, admin.PasswordHash))
            throw new AppException(ErrorCodes.AuthInvalidCredentials, "Invalid username or password.", 401);

        admin.LastLoginAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new AdminTokenResponse(_jwt.CreateAdminToken(admin.Id, admin.Username, admin.DisplayName),
            new AdminProfileDto(admin.Id, admin.Username, admin.DisplayName));
    }

    public async Task<AdminProfileDto> GetProfileAsync(Guid adminId, CancellationToken ct)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Id == adminId, ct)
            ?? throw AppException.NotFound("Admin not found.");
        if (!admin.IsActive)
            throw AppException.Forbidden("Admin account is inactive.");
        return new AdminProfileDto(admin.Id, admin.Username, admin.DisplayName);
    }
}
