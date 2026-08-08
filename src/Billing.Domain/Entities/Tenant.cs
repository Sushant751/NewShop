using Billing.Shared.Enums;

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a tenant (a shop / business). A tenant owns all of its data.
/// </summary>
public sealed class Tenant : Base.AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? CurrencyCode { get; set; } = "USD";
    public string? TimeZone { get; set; } = "UTC";
    public string? TaxIdentificationNumber { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public DateTime? TrialEndsOn { get; set; }
    public DateTime? SubscriptionEndsOn { get; set; }
    public Guid? PlanId { get; set; }
    public int MaxUsers { get; set; } = 5;
    public int MaxProducts { get; set; } = 1000;
}

/// <summary>
/// Subscription plan offered to tenants (e.g. Starter, Pro, Enterprise).
/// </summary>
public sealed class Plan : Base.AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int MaxShops { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A physical shop / branch belonging to a tenant. A tenant may have many shops.
/// </summary>
public sealed class Shop : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
