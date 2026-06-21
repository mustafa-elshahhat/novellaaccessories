using Microsoft.EntityFrameworkCore;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Domain.Entities;
using Novella.Domain.Enums;
using Novella.Domain.Services;

namespace Novella.Application.Auth;

/// <summary>
/// Owns OTP generation and verification (apps/api only — the WhatsApp sidecar never does this).
/// Enforces expiry, resend cooldown, attempt limits, resend limits, and temporary lockout.
/// Only OTP hashes are persisted; plain codes are returned transiently for rendering and never logged.
/// </summary>
public sealed class OtpService
{
    private readonly IAppDbContext _db;
    private readonly IOtpHasher _hasher;
    private readonly IClock _clock;

    public OtpService(IAppDbContext db, IOtpHasher hasher, IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
    }

    /// <summary>
    /// Issues (or resends) an OTP for the given normalized phone + purpose. Returns the plain code
    /// for immediate rendering only. Caller must NOT store or log it.
    /// </summary>
    public async Task<string> IssueAsync(string phone, string phoneNormalized, OtpPurpose purpose, Guid? customerId, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var existing = await _db.OtpCodes
            .Where(o => o.PhoneNumberNormalized == phoneNormalized && o.Purpose == purpose && o.UsedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var code = _hasher.GenerateCode();
        var codeHash = _hasher.Hash(code);
        var expiresAt = now.AddMinutes(OtpPolicy.ExpiryMinutes);

        if (existing is not null)
        {
            if (existing.LockedUntil is { } locked && locked > now)
                throw new AppException(ErrorCodes.OtpLocked, "Too many attempts. Try again later.", 429,
                    new Dictionary<string, object?> { ["lockedUntil"] = locked });

            var secondsSinceLastSend = (now - existing.LastSentAt).TotalSeconds;
            if (secondsSinceLastSend < OtpPolicy.ResendCooldownSeconds)
                throw new AppException(ErrorCodes.OtpCooldown, "Please wait before requesting another code.", 429,
                    new Dictionary<string, object?> { ["retryAfterSeconds"] = (int)(OtpPolicy.ResendCooldownSeconds - secondsSinceLastSend) });

            if (existing.ResendCount + 1 > OtpPolicy.MaxResends)
            {
                existing.LockedUntil = now.AddMinutes(OtpPolicy.LockoutMinutes);
                await _db.SaveChangesAsync(ct);
                throw new AppException(ErrorCodes.OtpResendLimit, "Resend limit reached. Try again later.", 429);
            }

            existing.CodeHash = codeHash;
            existing.ExpiresAt = expiresAt;
            existing.LastSentAt = now;
            existing.ResendCount += 1;
            existing.AttemptCount = 0;
            existing.RelatedCustomerId = customerId ?? existing.RelatedCustomerId;
            await _db.SaveChangesAsync(ct);
            return code;
        }

        _db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phone,
            PhoneNumberNormalized = phoneNormalized,
            Purpose = purpose,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            ResendCount = 0,
            LastSentAt = now,
            RelatedCustomerId = customerId,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);
        return code;
    }

    /// <summary>Verifies an OTP for phone + purpose. On success marks it used and returns the record.</summary>
    public async Task<OtpCode> VerifyAsync(string phoneNormalized, OtpPurpose purpose, string code, CancellationToken ct)
    {
        var now = _clock.UtcNow;

        var otp = await _db.OtpCodes
            .Where(o => o.PhoneNumberNormalized == phoneNormalized && o.Purpose == purpose && o.UsedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
            throw new AppException(ErrorCodes.OtpInvalid, "Invalid verification code.", 400);

        if (otp.LockedUntil is { } locked && locked > now)
            throw new AppException(ErrorCodes.OtpLocked, "Too many attempts. Try again later.", 429,
                new Dictionary<string, object?> { ["lockedUntil"] = locked });

        if (otp.ExpiresAt <= now)
            throw new AppException(ErrorCodes.OtpExpired, "Verification code has expired.", 400);

        if (otp.AttemptCount >= OtpPolicy.MaxAttempts)
        {
            otp.LockedUntil = now.AddMinutes(OtpPolicy.LockoutMinutes);
            await _db.SaveChangesAsync(ct);
            throw new AppException(ErrorCodes.OtpLocked, "Too many attempts. Try again later.", 429);
        }

        if (!_hasher.Verify(code, otp.CodeHash))
        {
            otp.AttemptCount += 1;
            if (otp.AttemptCount >= OtpPolicy.MaxAttempts)
                otp.LockedUntil = now.AddMinutes(OtpPolicy.LockoutMinutes);
            await _db.SaveChangesAsync(ct);
            throw new AppException(ErrorCodes.OtpInvalid, "Invalid verification code.", 400);
        }

        otp.UsedAt = now;
        await _db.SaveChangesAsync(ct);
        return otp;
    }
}
