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
var signingKey = string.IsNullOrWhiteSpace(jwt.SigningKey)
    ? "dev-only-insecure-signing-key-change-me-please-1234567890"
    : jwt.SigningKey;

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
var allowedOrigins = new[]
    {
        storefrontOrigin, adminOrigin,
        "http://localhost:3000", "http://localhost:5173", "http://localhost:5000"
    }
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
    /// <summary>Applies migrations and seeds defaults when a SQL Server connection is configured.</summary>
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        if (string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
            return;

        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            app.Logger.LogWarning("ConnectionStrings__DefaultConnection is not set; skipping migration and seeding.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        try
        {
            var db = sp.GetRequiredService<NovellaDbContext>();
            await db.Database.MigrateAsync();
            var seeder = sp.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
            app.Logger.LogInformation("Database migrated and seeded.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Database migration/seed failed at startup.");
        }
    }
}
