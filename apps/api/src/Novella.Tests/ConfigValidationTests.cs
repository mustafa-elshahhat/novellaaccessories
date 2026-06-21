using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Novella.Api.Configuration;
using Xunit;

namespace Novella.Tests;

/// <summary>Verifies the Production configuration fail-fast guard (StartupValidation).</summary>
public class ConfigValidationTests
{
    private const string ValidConn = "Server=db;Database=Novella;Trusted_Connection=True;TrustServerCertificate=True";
    private const string ValidKey = "A-Strong-Production-Jwt-Signing-Key-2026-xyz-9988!";

    private sealed class FakeEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Novella.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> ValidProd() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = ValidConn,
        ["Jwt:Issuer"] = "novella-api",
        ["Jwt:Audience"] = "novella-clients",
        ["Jwt:SigningKey"] = ValidKey,
        ["Cors:StorefrontOrigin"] = "https://novellaaccessories.store",
        ["Cors:AdminOrigin"] = "https://admin.novellaaccessories.store"
    };

    [Fact]
    public void Valid_production_config_passes()
    {
        var act = () => StartupValidation.ValidateProduction(Config(ValidProd()), new FakeEnv());
        act.Should().NotThrow();
    }

    [Fact]
    public void Development_skips_validation_even_when_empty()
    {
        var act = () => StartupValidation.ValidateProduction(Config(new()), new FakeEnv { EnvironmentName = "Development" });
        act.Should().NotThrow();
    }

    [Fact]
    public void Missing_connection_string_and_key_and_cors_throws()
    {
        var act = () => StartupValidation.ValidateProduction(Config(new()), new FakeEnv());
        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("ConnectionStrings:DefaultConnection");
        ex.Message.Should().Contain("Jwt:SigningKey");
        ex.Message.Should().Contain("Cors:StorefrontOrigin");
    }

    [Fact]
    public void Placeholder_signing_key_is_rejected()
    {
        var values = ValidProd();
        values["Jwt:SigningKey"] = "dev-only-insecure-signing-key-change-me-please-1234567890";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("placeholder");
    }

    [Fact]
    public void Short_signing_key_is_rejected()
    {
        var values = ValidProd();
        values["Jwt:SigningKey"] = "too-short";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("at least 32");
    }

    [Fact]
    public void Wildcard_cors_origin_is_rejected()
    {
        var values = ValidProd();
        values["Cors:StorefrontOrigin"] = "*";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("wildcard");
    }

    [Fact]
    public void AutoSeed_without_admin_password_is_rejected()
    {
        var values = ValidProd();
        values["Database:AutoSeed"] = "true";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("Seed:AdminPassword");
    }

    [Fact]
    public void Active_online_provider_without_webhook_secret_is_rejected()
    {
        var values = ValidProd();
        values["Payment:ActiveProvider"] = "BankCard";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("Payment:WebhookSecret");
    }

    [Fact]
    public void Cod_provider_without_webhook_secret_is_allowed()
    {
        var values = ValidProd();
        values["Payment:ActiveProvider"] = "CashOnDelivery";
        var act = () => StartupValidation.ValidateProduction(Config(values), new FakeEnv());
        act.Should().NotThrow();
    }
}
