namespace Novella.Application.Auth;

public sealed record RegisterRequest(string FullName, string PhoneNumber, string Password);
public sealed record VerifyPhoneRequest(string PhoneNumber, string Code);
public sealed record LoginRequest(string PhoneNumber, string Password);
public sealed record ForgotPasswordRequest(string PhoneNumber);
public sealed record ResetPasswordRequest(string PhoneNumber, string Code, string NewPassword);
public sealed record ChangePhoneRequest(string NewPhoneNumber);
public sealed record ChangePhoneVerifyRequest(string NewPhoneNumber, string Code);

public sealed record AuthTokenResponse(string Token, CustomerProfileDto Customer);
public sealed record RegisterResponse(bool RequiresVerification, string PhoneNumber);

public sealed record CustomerProfileDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    bool IsPhoneVerified,
    DateTime CreatedAt);

public sealed record AdminLoginRequest(string Username, string Password);
public sealed record AdminTokenResponse(string Token, AdminProfileDto Admin);
public sealed record AdminProfileDto(Guid Id, string Username, string DisplayName);
