using Billing.Application.DTOs.Auth;
using Billing.Shared.Results;

namespace Billing.Identity.Services;

/// <summary>
/// Application-level authentication service. Orchestrates login, token refresh,
/// registration, password change and password reset flows. This is the single
/// entry point used by the API controllers for identity concerns.
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);
    Task<Result<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
