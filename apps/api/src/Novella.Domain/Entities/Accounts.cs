using Novella.Domain.Enums;

namespace Novella.Domain.Entities;

/// <summary>A storefront customer account (phone + password, email not required).</summary>
public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneNumberNormalized { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsPhoneVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Pending request to change a customer's phone number (verified via OTP on the new number).</summary>
public class CustomerPhoneChangeRequest
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string OldPhoneNumber { get; set; } = string.Empty;
    public string NewPhoneNumber { get; set; } = string.Empty;
    public string NewPhoneNumberNormalized { get; set; } = string.Empty;
    public PhoneChangeStatus Status { get; set; } = PhoneChangeStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>Single back-office admin account for the MVP.</summary>
public class AdminUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// One-time password record. The plain code is never stored — only <see cref="CodeHash"/>.
/// Generation and verification are owned by apps/api; apps/whatsapp only delivers the text.
/// </summary>
public class OtpCode
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneNumberNormalized { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int AttemptCount { get; set; }
    public int ResendCount { get; set; }
    public DateTime LastSentAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public Guid? RelatedCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
}
