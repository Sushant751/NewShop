namespace Billing.Application.Abstractions;

/// <summary>
/// Abstraction over a distributed cache. Defined in the Application layer as a
/// port so handlers can depend on caching without referencing Infrastructure.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides access to the current authenticated user's identity, tenant, and
/// shop from the HTTP context. Defined here so application services can depend
/// on the caller's identity without referencing ASP.NET Core directly.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? ShopId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
