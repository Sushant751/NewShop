using System.Security.Claims;
using Billing.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using ClaimTypes = Billing.Shared.Constants.ClaimTypes;

namespace Billing.Identity.Authorization;

/// <summary>
/// Authorization requirement carrying a single permission string (e.g. "Products.Create").
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Authorization handler that checks whether the current user has the required
/// permission claim. Permissions are embedded in the JWT as "permission" claims.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // GlobalAdmin and ShopAdmin bypass all permission checks.
        // ShopAdmin has full access to their own tenant's data.
        if (context.User.IsInRole("GlobalAdmin") || context.User.IsInRole("ShopAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(ClaimTypes.Permission, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorization requirement for a specific role.
/// </summary>
public sealed class RoleRequirement : IAuthorizationRequirement
{
    public string Role { get; }

    public RoleRequirement(string role)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}

public sealed class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (context.User.IsInRole(requirement.Role) || context.User.IsInRole("GlobalAdmin"))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Convenience helpers for registering permission and role policies.
/// </summary>
public static class AuthorizationPolicies
{
    public const string PolicyPrefix = "Permission:";
    public const string RolePolicyPrefix = "Role:";

    public static string PermissionPolicy(string permission) => $"{PolicyPrefix}{permission}";
    public static string RolePolicy(string role) => $"{RolePolicyPrefix}{role}";
}
