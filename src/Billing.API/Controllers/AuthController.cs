using Billing.Application.DTOs.Auth;
using Billing.Identity.Services;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Authentication & authorization endpoints: login, register, refresh,
/// revoke, change password, forgot/reset password.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Authenticate a user and return access + refresh tokens.")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Register a new tenant and tenant admin user.")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, GetIpAddress(), GetDeviceInfo(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Exchange an expired access token + refresh token for a new pair.")]
    [ProducesResponseType(typeof(Result<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RefreshResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("revoke")]
    [Authorize]
    [SwaggerOperation(Summary = "Revoke a refresh token, ending the session.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeAsync(request.RefreshToken, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(Summary = "Logout by revoking the current refresh token.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    [SwaggerOperation(Summary = "Change the current user's password.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Result.Fail("User not authenticated."));

        var result = await _authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Request a password reset token (sent via email in production).")]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Reset a password using a reset token.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>Request body for token revocation / logout.</summary>
public sealed record RevokeRequest(string RefreshToken);
