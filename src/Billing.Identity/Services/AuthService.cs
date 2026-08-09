using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Billing.Application.DTOs.Auth;
using Billing.Domain.Entities;
using Billing.Identity.Options;
using Billing.Identity.Passwords;
using Billing.Identity.Tokens;
using Billing.Persistence.Repositories;
using Billing.Persistence.TenantContext;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Constants;
using Billing.Shared.Enums;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ClaimTypes = Billing.Shared.Constants.ClaimTypes;

namespace Billing.Identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITenantRepository tenantRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IJwtTokenService jwtTokenService,
        IPasswordHasherService passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tenantRepository = tenantRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        // 1. Resolve user by email globally (not tenant-scoped) so that registered
        //    users who belong to a different tenant can still log in.
        var userGlobal = await _userRepository.GetByEmailGlobalAsync(request.Email, null, cancellationToken);
        if (userGlobal is null || !userGlobal.IsActive)
            return Result<LoginResponse>.Fail("Invalid email or password.");

        // 2. Load their tenant.
        var tenant = await _tenantRepository.GetByIdAsync(userGlobal.TenantId, null, cancellationToken);
        if (tenant is null)
            return Result<LoginResponse>.Fail("Tenant not found.");

        if (tenant.Status == TenantStatus.Suspended || tenant.Status == TenantStatus.Terminated)
            return Result<LoginResponse>.Fail("Tenant account is suspended. Contact support.");

        // 3. Set tenant context so subsequent scoped queries work.
        _tenantContext.SetContext(tenant.Id, null, null, null);

        var user = userGlobal;

        if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            return Result<LoginResponse>.Fail($"Account is locked. Try again after {user.LockoutEnd.Value:u}.");

        // 4. Verify password.
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(user, ipAddress, cancellationToken);
            return Result<LoginResponse>.Fail("Invalid email or password.");
        }

        // 4. Load roles + permissions.
        var roles = await _userRepository.GetRolesAsync(user.Id, null, cancellationToken);
        var permissions = await _userRepository.GetPermissionsAsync(user.Id, null, cancellationToken);

        // 5. Generate tokens.
        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var (accessToken, jwtId, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user, tenant.Id, roles, permissions);
            var (refreshToken, refreshHash) = _jwtTokenService.GenerateRefreshToken();

            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                TokenHash = refreshHash,
                JwtId = jwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedByIp = ipAddress,
                DeviceInfo = deviceInfo,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.Id
            };
            await _refreshTokenRepository.InsertAsync(refresh, _unitOfWork.Transaction, cancellationToken);

            await _userRepository.UpdateLastLoginAsync(user.Id, ipAddress, deviceInfo, _unitOfWork.Transaction, cancellationToken);

            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                UserName = user.UserName,
                Action = "Login",
                EntityName = "User",
                EntityId = user.Id,
                IpAddress = ipAddress,
                UserAgent = deviceInfo
            }, _unitOfWork.Transaction, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            var tenantDisplayName = roles.Contains("GlobalAdmin") ? "App Admin" : tenant.Name;

            return Result<LoginResponse>.Ok(new LoginResponse(
                accessToken, refreshToken, accessExpiresAt,
                user.Id, tenant.Id, tenantDisplayName, user.UserName, user.Email, user.FullName,
                roles, permissions), "Login successful.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            return Result<RefreshResponse>.Fail("Invalid access token.");

        var tenantIdClaim = principal.FindFirst(ClaimTypes.TenantId)?.Value;
        var userIdClaim = principal.FindFirst(ClaimTypes.UserId)?.Value;
        var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (tenantIdClaim is null || userIdClaim is null || jtiClaim is null)
            return Result<RefreshResponse>.Fail("Invalid token claims.");

        var tenantId = Guid.Parse(tenantIdClaim);
        var userId = Guid.Parse(userIdClaim);

        _tenantContext.SetContext(tenantId, userId, null, null);

        var refreshHash = _jwtTokenService.HashToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(refreshHash, null, cancellationToken);
        if (stored is null || !stored.IsActive)
            return Result<RefreshResponse>.Fail("Invalid or expired refresh token.");

        if (stored.UserId != userId)
            return Result<RefreshResponse>.Fail("Refresh token does not belong to user.");

        if (stored.JwtId != jtiClaim)
            return Result<RefreshResponse>.Fail("Token mismatch.");

        var user = await _userRepository.GetByIdAsync(userId, null, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<RefreshResponse>.Fail("User is not active.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, null, cancellationToken);
        if (tenant is null || tenant.Status == TenantStatus.Suspended || tenant.Status == TenantStatus.Terminated)
            return Result<RefreshResponse>.Fail("Tenant is suspended.");

        var roles = await _userRepository.GetRolesAsync(userId, null, cancellationToken);
        var permissions = await _userRepository.GetPermissionsAsync(userId, null, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            // Rotate: revoke old, issue new.
            var newRefreshHash = _jwtTokenService.HashToken(request.RefreshToken);
            await _refreshTokenRepository.RevokeAsync(stored.Id, null, _unitOfWork.Transaction, cancellationToken);

            var (newAccessToken, newJwtId, newAccessExpires) = _jwtTokenService.GenerateAccessToken(user, tenantId, roles, permissions);
            var (newRefreshToken, newRefreshHashValue) = _jwtTokenService.GenerateRefreshToken();

            var newRefresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                TokenHash = newRefreshHashValue,
                JwtId = newJwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedByIp = stored.CreatedByIp,
                DeviceInfo = stored.DeviceInfo,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };
            await _refreshTokenRepository.InsertAsync(newRefresh, _unitOfWork.Transaction, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<RefreshResponse>.Ok(new RefreshResponse(newAccessToken, newRefreshToken, newAccessExpires), "Token refreshed.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = _jwtTokenService.HashToken(refreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(hash, null, cancellationToken);
        if (stored is null)
            return Result.Fail("Token not found.");

        _tenantContext.SetContext(stored.TenantId, stored.UserId, null, null);
        await _refreshTokenRepository.RevokeAsync(stored.Id, null, null, cancellationToken);
        return Result.Ok("Token revoked.");
    }

    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        // For self-service registration we create a brand-new tenant + admin user.
        var slug = GenerateSlug(request.TenantName ?? request.FullName);
        var existing = await _tenantRepository.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
            return Result<LoginResponse>.Fail("A tenant with that name already exists. Choose a different name.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.TenantName ?? request.FullName,
                Slug = slug,
                ContactEmail = request.Email,
                Status = TenantStatus.Trial,
                TrialEndsOn = DateTime.UtcNow.AddDays(14),
                CurrencyCode = "USD",
                TimeZone = "UTC",
                CreatedDate = DateTime.UtcNow
            };
            await _tenantRepository.InsertAsync(tenant, _unitOfWork.Transaction, cancellationToken);

            _tenantContext.SetContext(tenant.Id, null, null, null);

            var existingUser = await _userRepository.GetByEmailAsync(request.Email, _unitOfWork.Transaction, cancellationToken);
            if (existingUser is not null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<LoginResponse>.Fail("Email is already registered.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserName = request.Email,
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpperInvariant(),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                EmailConfirmed = false,
                IsActive = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };
            await _userRepository.InsertAsync(user, _unitOfWork.Transaction, cancellationToken);

            // Assign ShopAdmin role to the first user (tenant owner).
            var shopAdminRoleId = await GetOrCreateRoleIdAsync(Roles.ShopAdmin, tenant.Id, user.Id, _unitOfWork.Transaction, cancellationToken);
            await _userRepository.AssignRoleAsync(user.Id, shopAdminRoleId, _unitOfWork.Transaction, cancellationToken);

            var roles = new List<string> { Roles.ShopAdmin };
            var permissions = (IReadOnlyList<string>)Permissions.All;

            var (accessToken, jwtId, accessExpires) = _jwtTokenService.GenerateAccessToken(user, tenant.Id, roles, permissions);
            var (refreshToken, refreshHash) = _jwtTokenService.GenerateRefreshToken();

            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                TokenHash = refreshHash,
                JwtId = jwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedByIp = ipAddress,
                DeviceInfo = deviceInfo,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.Id
            };
            await _refreshTokenRepository.InsertAsync(refresh, _unitOfWork.Transaction, cancellationToken);

            await _userRepository.UpdateLastLoginAsync(user.Id, ipAddress, deviceInfo, _unitOfWork.Transaction, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<LoginResponse>.Ok(new LoginResponse(
                accessToken, refreshToken, accessExpires,
                user.Id, tenant.Id, tenant.Name, user.UserName, user.Email, user.FullName,
                roles, permissions), "Registration successful.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, null, cancellationToken);
        if (user is null)
            return Result.Fail("User not found.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Fail("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = userId;
        await _userRepository.UpdateAsync(user, null, cancellationToken);

        // Revoke all refresh tokens so the user must sign in again.
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, null, cancellationToken);

        return Result.Ok("Password changed. Please sign in again.");
    }

    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Always return success to avoid leaking which emails exist.
        var tenants = await _tenantRepository.GetActiveAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            _tenantContext.SetContext(tenant.Id, null, null, null);
            var user = await _userRepository.GetByEmailAsync(request.Email, null, cancellationToken);
            if (user is not null && user.IsActive)
            {
                // In a real system, generate a token and email it. Here we return a reset token.
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                user.SecurityStamp = token;
                user.UpdatedDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, null, cancellationToken);
                _logger.LogInformation("Password reset requested for {Email}", request.Email);
                return Result<string>.Ok(token, "Reset instructions sent.");
            }
        }
        return Result<string>.Ok(string.Empty, "If the email exists, reset instructions have been sent.");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetActiveAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            _tenantContext.SetContext(tenant.Id, null, null, null);
            var user = await _userRepository.GetByEmailAsync(request.Email, null, cancellationToken);
            if (user is not null && user.IsActive && user.SecurityStamp == request.Token)
            {
                user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                user.SecurityStamp = Guid.NewGuid().ToString("N");
                user.UpdatedDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, null, cancellationToken);
                await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, null, cancellationToken);
                return Result.Ok("Password reset successful.");
            }
        }
        return Result.Fail("Invalid or expired reset token.");
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await RevokeAsync(refreshToken, cancellationToken);
    }

    private async Task RecordFailedLoginAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        if (user.AccessFailedCount >= _jwtSettings.MaxFailedAttempts)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.LockoutMinutes);
            user.AccessFailedCount = 0;
        }
        user.UpdatedDate = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, null, cancellationToken);

        await _auditLogRepository.LogAsync(new AuditLog
        {
            UserId = user.Id,
            UserName = user.UserName,
            Action = "FailedLogin",
            EntityName = "User",
            EntityId = user.Id,
            IpAddress = ipAddress
        }, null, cancellationToken);
    }

    private async Task<Guid> GetOrCreateRoleIdAsync(string roleName, Guid tenantId, Guid createdBy,
        System.Data.IDbTransaction transaction, CancellationToken cancellationToken)
    {
        // Look up the role by name within the tenant.
        const string sql = "SELECT TOP 1 Id FROM Roles WHERE TenantId = @TenantId AND NormalizedName = @Name AND IsDeleted = 0";
        var connection = _unitOfWork.Connection;
        var existing = await Dapper.SqlMapper.QueryFirstOrDefaultAsync<Guid?>(
            connection,
            new Dapper.CommandDefinition(sql, new { TenantId = tenantId, Name = roleName.ToUpperInvariant() },
            transaction, cancellationToken: cancellationToken));
        if (existing.HasValue)
            return existing.Value;

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            IsSystemRole = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        await Dapper.SqlMapper.ExecuteAsync(connection, new Dapper.CommandDefinition(
            @"INSERT INTO Roles (Id, TenantId, Name, NormalizedName, IsSystemRole, CreatedDate, CreatedBy, IsDeleted)
              VALUES (@Id, @TenantId, @Name, @NormalizedName, @IsSystemRole, @CreatedDate, @CreatedBy, 0)",
            role, transaction, cancellationToken: cancellationToken));
        return role.Id;
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = string.Concat(slug.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = "tenant";
        slug += "-" + Guid.NewGuid().ToString("N")[..6];
        return slug;
    }
}
