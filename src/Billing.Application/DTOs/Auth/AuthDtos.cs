namespace Billing.Application.DTOs.Auth;

public sealed record LoginRequest(string Email, string Password, string? TenantSlug = null);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    Guid TenantId,
    string TenantName,
    string UserName,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record RefreshRequest(string AccessToken, string RefreshToken);

public sealed record RefreshResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed record RegisterRequest(
    string FullName, string Email, string Password, string? PhoneNumber, string? TenantName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record UserDto(
    Guid Id, Guid TenantId, string UserName, string Email, string FullName,
    string? PhoneNumber, bool IsActive, Guid? ShopId, DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);
