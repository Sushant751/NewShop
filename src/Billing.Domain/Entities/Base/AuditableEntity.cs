namespace Billing.Domain.Entities.Base;

/// <summary>
/// Base class for every multi-tenant entity. Mirrors the audit columns
/// present on every table in the database (TenantId, CreatedBy, etc.).
/// </summary>
public abstract class AuditableTenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Base class for global (non-tenant-scoped) entities such as Tenants and
/// global settings. Still carries audit columns.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }
}
