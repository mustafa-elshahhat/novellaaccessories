namespace Novella.Domain.Services;

/// <summary>OTP lifecycle policy constants enforced by apps/api.</summary>
public static class OtpPolicy
{
    public const int CodeLength = 6;
    public const int ExpiryMinutes = 10;
    public const int ResendCooldownSeconds = 60;
    public const int MaxAttempts = 5;
    public const int MaxResends = 5;
    public const int LockoutMinutes = 30;
}

/// <summary>Bounded WhatsApp retry policy.</summary>
public static class WhatsAppPolicy
{
    public const int MaxRetries = 5;
}

/// <summary>Number of Delivered orders that triggers the reward coupon.</summary>
public static class RewardPolicy
{
    public const int DeliveredOrdersForReward = 2;
}
