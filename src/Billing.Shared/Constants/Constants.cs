namespace Billing.Shared.Constants;

/// <summary>
/// Well-known claim types used in JWT tokens and authorization policies.
/// </summary>
public static class ClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string UserId = "user_id";
    public const string ShopId = "shop_id";
    public const string Permission = "permission";
    public const string Role = "role";
}

/// <summary>
/// Cache key builders used by the Redis cache service.
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "billing";

    public static string Product(Guid tenantId, Guid productId) => $"{Prefix}:product:{tenantId}:{productId}";
    public static string ProductList(Guid tenantId, int page, int size, string? search) =>
        $"{Prefix}:products:{tenantId}:p{page}:s{size}:q{search ?? string.Empty}";
    public static string CustomerList(Guid tenantId, int page, int size) =>
        $"{Prefix}:customers:{tenantId}:p{page}:s{size}";
    public static string Dashboard(Guid tenantId) => $"{Prefix}:dashboard:{tenantId}";
    public static string UserPermissions(Guid userId) => $"{Prefix}:perms:{userId}";
    public static string Tenant(Guid tenantId) => $"{Prefix}:tenant:{tenantId}";
}

/// <summary>
/// Default pagination values.
/// </summary>
public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
