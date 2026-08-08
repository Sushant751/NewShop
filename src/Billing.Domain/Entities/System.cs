namespace Billing.Domain.Entities;

public sealed class AuditLog : Base.AuditableTenantEntity
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class ActivityLog : Base.AuditableTenantEntity
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class Setting : Base.AuditableTenantEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Group { get; set; }
    public string? Description { get; set; }
}

public sealed class Notification : Base.AuditableTenantEntity
{
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public string? Link { get; set; }
}
