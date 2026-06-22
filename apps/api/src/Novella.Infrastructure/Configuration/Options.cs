namespace Novella.Infrastructure.Configuration;

/// <summary>JWT options bound from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "novella";
    public string Audience { get; set; } = "novella-clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryDays { get; set; } = 7;
    public int AdminExpiryMinutes { get; set; } = 60;
}

/// <summary>Cloudinary options bound from the <c>Cloudinary</c> configuration section.</summary>
public sealed class CloudinaryOptions
{
    public const string Section = "Cloudinary";
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(CloudName)
                                && !string.IsNullOrWhiteSpace(ApiKey)
                                && !string.IsNullOrWhiteSpace(ApiSecret);
}

/// <summary>apps/whatsapp sidecar options bound from the <c>WhatsApp</c> configuration section.</summary>
public sealed class WhatsAppOptions
{
    public const string Section = "WhatsApp";
    public string BaseUrl { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(InternalApiKey);
}

/// <summary>Payment options bound from the <c>Payment</c> configuration section.</summary>
public sealed class PaymentOptions
{
    public const string Section = "Payment";
    public string? ActiveProvider { get; set; }
    public string? WebhookSecret { get; set; }
}

/// <summary>Seed options (admin bootstrap credentials) bound from the <c>Seed</c> section.</summary>
public sealed class SeedOptions
{
    public const string Section = "Seed";
    public string AdminUsername { get; set; } = "admin";
    public string? AdminPassword { get; set; }
    public string AdminDisplayName { get; set; } = "Store Admin";
}

/// <summary>
/// Controls startup database lifecycle. Both default to <c>false</c> so Production never migrates or
/// seeds implicitly; an environment must opt in explicitly. Migration and seeding are independent.
/// </summary>
public sealed class DatabaseOptions
{
    public const string Section = "Database";
    public bool AutoMigrate { get; set; }
    public bool AutoSeed { get; set; }
}
