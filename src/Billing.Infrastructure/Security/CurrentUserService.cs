using System.Security.Claims;
using Billing.Application.Abstractions;
using Billing.Persistence.TenantContext;
using Billing.Shared.Constants;
using Microsoft.AspNetCore.Http;
using ClaimTypes = Billing.Shared.Constants.ClaimTypes;

namespace Billing.Infrastructure.Security;

/// <summary>
/// Provides access to the current authenticated user's identity, tenant, and
/// shop from the HTTP context. Implements the Application-layer abstraction so
/// handlers can depend on <see cref="ICurrentUserService"/> without referencing
/// Infrastructure.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ITenantContext _tenantContext;

    public CurrentUserService(IHttpContextAccessor accessor, ITenantContext tenantContext)
    {
        _accessor = accessor;
        _tenantContext = tenantContext;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;
    private HttpContext? Context => _accessor.HttpContext;

    public Guid? UserId => _tenantContext.UserId;
    public Guid? TenantId => _tenantContext.TenantId;
    public Guid? ShopId => _tenantContext.ShopId;
    public string? UserName => _tenantContext.UserName;
    public string? Email => User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;

    public bool HasPermission(string permission) =>
        User?.HasClaim(ClaimTypes.Permission, permission) == true
        || User?.HasClaim("permission", permission) == true;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> Permissions =>
        User?.FindAll(ClaimTypes.Permission).Select(c => c.Value).ToList()
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();
}
