namespace Billing.Domain.Entities;

/// <summary>
/// Application user. Authentication is handled by ASP.NET Core Identity; this
/// entity mirrors the user record and carries tenant + shop scoping.
/// </summary>
public sealed class User : Base.AuditableTenantEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ShopId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public string? DeviceInfo { get; set; }
}

public sealed class Role : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
}

public sealed class Permission : Base.AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
}

public sealed class RolePermission : Base.AuditableTenantEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class UserRole : Base.AuditableTenantEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public sealed class RefreshToken : Base.AuditableTenantEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? JwtId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
