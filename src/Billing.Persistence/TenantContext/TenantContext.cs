namespace Billing.Persistence.TenantContext;

/// <summary>
/// Holds the tenant / user context for the current request. Populated by
/// middleware from the authenticated user's claims and injected into every
/// repository so that queries are automatically scoped to the tenant.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    Guid? ShopId { get; }
    string? UserName { get; }
    bool IsAvailable { get; }

    void SetContext(Guid tenantId, Guid? userId, Guid? shopId, string? userName);
    void Clear();
}

/// <summary>
/// Scoped implementation - one instance per HTTP request.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ShopId { get; private set; }
    public string? UserName { get; private set; }
    public bool IsAvailable => TenantId.HasValue;

    public void SetContext(Guid tenantId, Guid? userId, Guid? shopId, string? userName)
    {
        TenantId = tenantId;
        UserId = userId;
        ShopId = shopId;
        UserName = userName;
    }

    public void Clear()
    {
        TenantId = null;
        UserId = null;
        ShopId = null;
        UserName = null;
    }
}
