using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Novella.Api.Auth;
using Novella.Application.Abstractions;
using Novella.Application.Auth;

namespace Novella.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(AuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
        => Ok(await _auth.RegisterAsync(req, ct));

    [HttpPost("verify-phone")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest req, CancellationToken ct)
        => Ok(await _auth.VerifyPhoneAsync(req, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        => Ok(await _auth.LoginAsync(req, ct));

    [HttpPost("logout")]
    [Authorize(Policy = "Customer")]
    public IActionResult Logout() => Ok(new { success = true });

    [HttpPost("forgot-password/request-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordRequest([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        await _auth.RequestPasswordResetAsync(req, ct);
        return Ok(new { success = true });
    }

    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordReset([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(req, ct);
        return Ok(new { success = true });
    }

    [HttpPost("change-phone/request-otp")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> ChangePhoneRequest([FromBody] ChangePhoneRequest req, CancellationToken ct)
    {
        await _auth.RequestPhoneChangeAsync(_currentUser.RequireCustomerId(), req, ct);
        return Ok(new { success = true });
    }

    [HttpPost("change-phone/verify")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> ChangePhoneVerify([FromBody] ChangePhoneVerifyRequest req, CancellationToken ct)
    {
        await _auth.VerifyPhoneChangeAsync(_currentUser.RequireCustomerId(), req, ct);
        return Ok(new { success = true });
    }

    [HttpGet("me")]
    [Authorize(Policy = "Customer")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await _auth.GetProfileAsync(_currentUser.RequireCustomerId(), ct));
}

[ApiController]
[Route("api/admin/auth")]
[EnableRateLimiting("auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly AdminAuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AdminAuthController(AdminAuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest req, CancellationToken ct)
        => Ok(await _auth.LoginAsync(req, ct));

    [HttpPost("logout")]
    [Authorize(Policy = "Admin")]
    public IActionResult Logout() => Ok(new { success = true });

    [HttpGet("me")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await _auth.GetProfileAsync(_currentUser.RequireAdminId(), ct));
}
