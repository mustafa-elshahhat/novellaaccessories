using Novella.Domain.Enums;

namespace Novella.Application.Abstractions;

/// <summary>Strong password hashing (BCrypt in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Generates and verifies OTP codes; only hashes are persisted.</summary>
public interface IOtpHasher
{
    /// <summary>Generates a numeric OTP of the configured length.</summary>
    string GenerateCode();
    string Hash(string code);
    bool Verify(string code, string hash);
}

/// <summary>Issues signed JWTs for customers and the admin.</summary>
public interface IJwtTokenService
{
    string CreateCustomerToken(Guid customerId, string phone, string fullName);
    string CreateAdminToken(Guid adminId, string username, string displayName);
}

/// <summary>Abstracts the system clock (always UTC) for testability.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Accessor for the authenticated principal on the current request.</summary>
public interface ICurrentUser
{
    Guid? CustomerId { get; }
    Guid? AdminId { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
