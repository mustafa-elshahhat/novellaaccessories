namespace Novella.Api.Configuration;

/// <summary>
/// Fails startup fast and clearly when mandatory Production configuration is missing or unsafe.
/// Runs only in the Production environment; Development/Testing keep their local fallbacks.
/// Never echoes secret values — only the offending key name and the reason are reported.
/// </summary>
public static class StartupValidation
{
    /// <summary>Obvious placeholder / development signing-key markers that must never reach Production.</summary>
    private static readonly string[] PlaceholderMarkers =
    {
        "dev-only", "change-me", "changeme", "your-signing-key", "placeholder",
        "insecure", "example", "todo", "sample", "test-key"
    };

    private const int MinSigningKeyLength = 32;

    public static void ValidateProduction(IConfiguration config, IHostEnvironment env)
    {
        if (!env.IsProduction()) return;

        var errors = new List<string>();

        // ---- SQL Server connection ----
        if (string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")))
            errors.Add("ConnectionStrings:DefaultConnection is required in Production.");

        // ---- JWT ----
        var jwt = config.GetSection("Jwt");
        if (string.IsNullOrWhiteSpace(jwt["Issuer"]))
            errors.Add("Jwt:Issuer is required in Production.");
        if (string.IsNullOrWhiteSpace(jwt["Audience"]))
            errors.Add("Jwt:Audience is required in Production.");

        var signingKey = jwt["SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
            errors.Add("Jwt:SigningKey is required in Production.");
        else if (signingKey.Trim().Length < MinSigningKeyLength)
            errors.Add($"Jwt:SigningKey must be at least {MinSigningKeyLength} characters in Production.");
        else if (PlaceholderMarkers.Any(m => signingKey.Contains(m, StringComparison.OrdinalIgnoreCase)))
            errors.Add("Jwt:SigningKey looks like a placeholder/development value and must not be used in Production.");

        // ---- CORS (no wildcard, must be valid absolute origins) ----
        ValidateOrigin(config, "Cors:StorefrontOrigin", errors);
        ValidateOrigin(config, "Cors:AdminOrigin", errors);

        // ---- Admin seed password (only when auto-seed will run) ----
        if (config.GetValue("Database:AutoSeed", false) &&
            string.IsNullOrWhiteSpace(config["Seed:AdminPassword"]))
            errors.Add("Seed:AdminPassword is required in Production when Database:AutoSeed is enabled.");

        // ---- Payment webhook secret (only when a non-COD provider is active) ----
        var activeProvider = config["Payment:ActiveProvider"];
        if (!string.IsNullOrWhiteSpace(activeProvider) && !IsCod(activeProvider) &&
            string.IsNullOrWhiteSpace(config["Payment:WebhookSecret"]))
            errors.Add($"Payment:WebhookSecret is required in Production when Payment:ActiveProvider='{activeProvider}'.");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Invalid Production configuration:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
    }

    private static void ValidateOrigin(IConfiguration config, string key, List<string> errors)
    {
        var value = config[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{key} is required in Production.");
            return;
        }

        var trimmed = value.Trim();
        if (trimmed == "*" || trimmed.Contains('*'))
            errors.Add($"{key} must not be a wildcard origin in Production.");
        else if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            errors.Add($"{key} must be a valid absolute http(s) origin in Production.");
    }

    private static bool IsCod(string provider) =>
        provider.Equals("COD", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("CashOnDelivery", StringComparison.OrdinalIgnoreCase) ||
        provider.Equals("Cash", StringComparison.OrdinalIgnoreCase);
}
