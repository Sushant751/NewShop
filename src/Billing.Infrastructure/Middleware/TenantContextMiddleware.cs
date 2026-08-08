using System.Security.Claims;
using Billing.Persistence.TenantContext;
using Billing.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ClaimTypes = Billing.Shared.Constants.ClaimTypes;

namespace Billing.Infrastructure.Middleware;

/// <summary>
/// Middleware that resolves the tenant / user context from the authenticated
/// user's JWT claims and populates the scoped <see cref="ITenantContext"/>.
/// Every repository relies on this context to scope queries to the tenant.
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = user.FindFirst(ClaimTypes.TenantId)?.Value
                ?? user.FindFirst("tenant_id")?.Value;
            var userIdClaim = user.FindFirst(ClaimTypes.UserId)?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var shopIdClaim = user.FindFirst(ClaimTypes.ShopId)?.Value;
            var userName = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? user.Identity?.Name;

            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : (Guid?)null;
                var shopId = Guid.TryParse(shopIdClaim, out var sid) ? sid : (Guid?)null;
                tenantContext.SetContext(tenantId, userId, shopId, userName);

                using (LogContext.PushProperty("TenantId", tenantId))
                using (LogContext.PushProperty("UserId", userId))
                {
                    await _next(context);
                    return;
                }
            }

            _logger.LogWarning("Authenticated request missing TenantId claim. Path: {Path}", context.Request.Path);
        }

        await _next(context);
    }
}
