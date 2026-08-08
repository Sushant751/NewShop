using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Users") { }

    public async Task<User?> GetByEmailAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Users WHERE TenantId = @TenantId AND NormalizedEmail = @Email AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql, new { TenantId = tenantId, Email = email.ToUpperInvariant() }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailGlobalAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // No tenant filter — used during login to find the user regardless of which tenant they belong to.
        const string sql = "SELECT * FROM Users WHERE NormalizedEmail = @Email AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email.ToUpperInvariant() }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByUserNameAsync(string userName, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Users WHERE TenantId = @TenantId AND UserName = @UserName AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<User>(
            new CommandDefinition(sql, new { TenantId = tenantId, UserName = userName }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT DISTINCT p.Name
            FROM UserRoles ur
            INNER JOIN RolePermissions rp ON rp.RoleId = ur.RoleId AND rp.TenantId = ur.TenantId
            INNER JOIN Permissions p ON p.Id = rp.PermissionId
            WHERE ur.TenantId = @TenantId AND ur.UserId = @UserId AND ur.IsDeleted = 0
              AND rp.IsDeleted = 0 AND p.IsDeleted = 0;";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT r.Name
            FROM UserRoles ur
            INNER JOIN Roles r ON r.Id = ur.RoleId AND r.TenantId = ur.TenantId
            WHERE ur.TenantId = @TenantId AND ur.UserId = @UserId AND ur.IsDeleted = 0 AND r.IsDeleted = 0;";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { TenantId = tenantId, UserId = userId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<Guid> GetRoleIdByNameAsync(string roleName, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id FROM Roles WHERE Name = @RoleName AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Guid>(
            new CommandDefinition(sql, new { RoleName = roleName, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId AND TenantId = @TenantId AND IsDeleted = 0)
            INSERT INTO UserRoles (Id, TenantId, UserId, RoleId, CreatedDate, CreatedBy, IsDeleted)
            VALUES (NEWID(), @TenantId, @UserId, @RoleId, @Now, @CreatedBy, 0);";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { TenantId = tenantId, UserId = userId, RoleId = roleId, Now = DateTime.UtcNow, CreatedBy = TenantContext.UserId },
            transaction, cancellationToken: cancellationToken));
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "UPDATE UserRoles SET IsDeleted = 1 WHERE UserId = @UserId AND RoleId = @RoleId AND TenantId = @TenantId";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { TenantId = tenantId, UserId = userId, RoleId = roleId },
            transaction, cancellationToken: cancellationToken));
    }

    public async Task UpdateLastLoginAsync(Guid userId, string? ip, string? deviceInfo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "UPDATE Users SET LastLoginAt = @Now, LastLoginIp = @Ip, DeviceInfo = @DeviceInfo WHERE Id = @UserId AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, TenantId = tenantId, Now = DateTime.UtcNow, Ip = ip, DeviceInfo = deviceInfo },
            transaction, cancellationToken: cancellationToken));
    }
}

public sealed class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "RefreshTokens") { }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM RefreshTokens WHERE TenantId = @TenantId AND TokenHash = @TokenHash AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
            new CommandDefinition(sql, new { TenantId = tenantId, TokenHash = tokenHash }, transaction, cancellationToken: cancellationToken));
    }

    public async Task RevokeAsync(Guid tokenId, string? replacedByToken, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "UPDATE RefreshTokens SET RevokedAt = @Now, ReplacedByToken = @ReplacedByToken WHERE Id = @TokenId AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { TokenId = tokenId, TenantId = tenantId, Now = DateTime.UtcNow, ReplacedByToken = replacedByToken },
            transaction, cancellationToken: cancellationToken));
    }

    public async Task RevokeAllForUserAsync(Guid userId, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "UPDATE RefreshTokens SET RevokedAt = @Now WHERE UserId = @UserId AND TenantId = @TenantId AND RevokedAt IS NULL AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql,
            new { UserId = userId, TenantId = tenantId, Now = DateTime.UtcNow },
            transaction, cancellationToken: cancellationToken));
    }
}

public sealed class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "AuditLogs") { }

    public async Task LogAsync(AuditLog log, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (log.Id == Guid.Empty) log.Id = Guid.NewGuid();
        log.TenantId = tenantId;
        log.CreatedDate = DateTime.UtcNow;
        log.CreatedBy ??= TenantContext.UserId;

        const string sql = @"
            INSERT INTO AuditLogs
                (Id, TenantId, UserId, UserName, Action, EntityName, EntityId,
                 OldValues, NewValues, IpAddress, UserAgent, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @UserId, @UserName, @Action, @EntityName, @EntityId,
                 @OldValues, @NewValues, @IpAddress, @UserAgent, @CreatedDate, @CreatedBy, 0);";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, log, transaction, cancellationToken: cancellationToken));
    }
}
