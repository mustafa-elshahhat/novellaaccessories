using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Novella.Application.Abstractions;

namespace Novella.Api.Auth;

/// <summary>Reads the authenticated principal (customer or admin) from the current request.</summary>
public sealed class CurrentUser : ICurrentUser
{
    public const string RoleCustomer = "customer";
    public const string RoleAdmin = "admin";

    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor) => _principal = accessor.HttpContext?.User;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => IsAuthenticated && _principal!.IsInRole(RoleAdmin);

    public Guid? AdminId => IsAdmin ? SubjectId : null;

    public Guid? CustomerId => IsAuthenticated && _principal!.IsInRole(RoleCustomer) ? SubjectId : null;

    private Guid? SubjectId
    {
        get
        {
            var sub = _principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? _principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}

/// <summary>Convenience accessors that throw a 401 when the required principal is absent.</summary>
public static class CurrentUserExtensions
{
    public static Guid RequireCustomerId(this ICurrentUser user)
        => user.CustomerId ?? throw Application.Common.AppException.Unauthorized("Customer authentication required.");

    public static Guid RequireAdminId(this ICurrentUser user)
        => user.AdminId ?? throw Application.Common.AppException.Unauthorized("Admin authentication required.");
}
