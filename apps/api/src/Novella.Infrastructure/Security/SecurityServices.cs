using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Domain.Services;
using Novella.Infrastructure.Configuration;

namespace Novella.Infrastructure.Security;

/// <summary>BCrypt password hashing.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}

/// <summary>
/// OTP generation + hashing. Codes are generated with a cryptographic RNG and stored only as
/// salted hashes (never plain text, never logged).
/// </summary>
public sealed class OtpHasher : IOtpHasher
{
    public string GenerateCode()
    {
        var max = (int)Math.Pow(10, OtpPolicy.CodeLength);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString().PadLeft(OtpPolicy.CodeLength, '0');
    }

    public string Hash(string code) => BCrypt.Net.BCrypt.HashPassword(code, workFactor: 10);

    public bool Verify(string code, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(code, hash); }
        catch { return false; }
    }
}

/// <summary>System clock (UTC).</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>JWT issuance for customer and admin principals.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    public const string RoleCustomer = "customer";
    public const string RoleAdmin = "admin";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string CreateCustomerToken(Guid customerId, string phone, string fullName)
        => Create(customerId, RoleCustomer, new[]
        {
            new Claim(ClaimTypes.MobilePhone, phone),
            new Claim("name", fullName)
        });

    public string CreateAdminToken(Guid adminId, string username, string displayName)
        => Create(adminId, RoleAdmin, new[]
        {
            new Claim("username", username),
            new Claim("name", displayName)
        });

    private string Create(Guid subjectId, string role, IEnumerable<Claim> extra)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };
        claims.AddRange(extra);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(_options.ExpiryDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
