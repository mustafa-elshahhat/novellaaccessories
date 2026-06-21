using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Novella.Api.Auth;
using Novella.Api.Common;
using Novella.Api.Configuration;
using Novella.Api.Middleware;
using Novella.Application;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Infrastructure;
using Novella.Infrastructure.Configuration;
using Novella.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Environment variables provide Section__Key configuration (e.g. Jwt__SigningKey).
builder.Configuration.AddEnvironmentVariables();

// Fail fast in Production when mandatory configuration is missing or unsafe.
StartupValidation.ValidateProduction(builder.Configuration, builder.Environment);

// ---- Controllers + JSON (enums as strings) + standard validation error shape ----
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
        var error = new ApiError
        {
            Code = ErrorCodes.ValidationError,
            Message = "One or more validation errors occurred.",
            Details = errors
        };
        return new BadRequestObjectResult(error);
    };
});

builder.Services.AddOpenApi();

// ---- Application + Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Current user ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---- AuthN / AuthZ ----
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
// A real signing key is mandatory in Production (enforced by StartupValidation above). Only the
// local development/testing host may fall back to a clearly-labelled insecure key.
var signingKey = !string.IsNullOrWhiteSpace(jwt.SigningKey)
    ? jwt.SigningKey
    : builder.Environment.IsProduction()
        ? throw new InvalidOperationException("Jwt:SigningKey is required in Production.")
        : "dev-only-insecure-signing-key-change-me-please-1234567890";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Customer", p => p.RequireRole(CurrentUser.RoleCustomer))
    .AddPolicy("Admin", p => p.RequireRole(CurrentUser.RoleAdmin));

// ---- CORS (environment-driven; no wildcard in production) ----
const string corsPolicy = "NovellaCors";
var storefrontOrigin = builder.Configuration["Cors:StorefrontOrigin"];
var adminOrigin = builder.Configuration["Cors:AdminOrigin"];
// Localhost dev origins are only trusted outside Production; Production uses the configured origins only.
var localDevOrigins = builder.Environment.IsProduction()
    ? Array.Empty<string>()
    : new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:5000" };
var allowedOrigins = new[] { storefrontOrigin, adminOrigin }
    .Concat(localDevOrigins)
    .Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o!).Distinct().ToArray();

builder.Services.AddCors(options =>
    options.AddPolicy(corsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ---- Rate limiting for auth/OTP endpoints (per IP) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

await app.MigrateAndSeedAsync();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(corsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "novella-api", timeUtc = DateTime.UtcNow }))
    .AllowAnonymous();

app.MapControllers();

app.Run();

/// <summary>Exposes the Program type for WebApplicationFactory-based integration tests.</summary>
public partial class Program { }

internal static class StartupExtensions
{
    /// <summary>
    /// Applies migrations and/or seeds defaults ONLY when explicitly enabled via
    /// <c>Database:AutoMigrate</c> / <c>Database:AutoSeed</c> (both default <c>false</c>). Migration
    /// and seeding are controlled independently. The connection string and seed secrets are never logged.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        if (string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            return;

        var options = app.Configuration.GetSection(DatabaseOptions.Section).Get<DatabaseOptions>() ?? new DatabaseOptions();
        var logger = app.Logger;

        if (!options.AutoMigrate && !options.AutoSeed)
        {
            logger.LogInformation(
                "Database lifecycle skipped: AutoMigrate and AutoSeed are both disabled (set Database:AutoMigrate / Database:AutoSeed to enable).");
            return;
        }

        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("ConnectionStrings:DefaultConnection is not set; skipping migration and seeding.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<NovellaDbContext>();

        if (options.AutoMigrate)
        {
            logger.LogInformation("Database AutoMigrate enabled: applying migrations...");
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Database AutoMigrate completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database AutoMigrate failed at startup.");
                throw;
            }
        }
        else
        {
            logger.LogInformation("Database AutoMigrate disabled: skipping migrations.");
        }

        if (options.AutoSeed)
        {
            logger.LogInformation("Database AutoSeed enabled: seeding defaults (idempotent)...");
            try
            {
                var seeder = sp.GetRequiredService<DataSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("Database AutoSeed completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database AutoSeed failed at startup.");
                throw;
            }
        }
        else
        {
            logger.LogInformation("Database AutoSeed disabled: skipping seeding.");
        }
    }
}
