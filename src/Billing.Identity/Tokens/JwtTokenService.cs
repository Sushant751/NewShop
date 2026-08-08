using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Billing.Domain.Entities;
using Billing.Identity.Options;
using Billing.Shared.Constants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ClaimTypes = Billing.Shared.Constants.ClaimTypes;

namespace Billing.Identity.Tokens;

/// <summary>
/// Generates JWT access tokens and opaque refresh tokens. Access tokens carry
/// tenant_id, user_id, shop_id, role and permission claims so that downstream
/// middleware and authorization handlers can enforce multi-tenant isolation
/// and fine-grained permissions without a database round-trip.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT access token for the supplied user + claims.</summary>
    (string Token, string JwtId, DateTime ExpiresAt) GenerateAccessToken(
        User user,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions);

    /// <summary>Generates a cryptographically random refresh token (plain text).</summary>
    (string Token, string Hash) GenerateRefreshToken();

    /// <summary>Validates a token and returns its claims principal, or null.</summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>SHA-256 hash of a refresh token for safe storage.</summary>
    string HashToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string Token, string JwtId, DateTime ExpiresAt) GenerateAccessToken(
        User user,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(ClaimTypes.UserId, user.Id.ToString()),
            new(ClaimTypes.TenantId, tenantId.ToString()),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.UserName),
            new(System.Security.Claims.ClaimTypes.Email, user.Email)
        };

        if (user.ShopId.HasValue)
            claims.Add(new Claim(ClaimTypes.ShopId, user.ShopId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(System.Security.Claims.ClaimTypes.Role, role));

        foreach (var permission in permissions.Distinct())
            claims.Add(new Claim(ClaimTypes.Permission, permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), jwtId, expiresAt);
    }

    public (string Token, string Hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        return (token, HashToken(token));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false // we intentionally accept expired tokens for refresh
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
