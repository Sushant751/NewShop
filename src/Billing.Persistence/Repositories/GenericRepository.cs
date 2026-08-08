using System.Data;
using System.Reflection;
using System.Text;
using Billing.Domain.Entities.Base;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

/// <summary>
/// Dapper-backed generic repository for tenant-scoped entities. Generates
/// parameterized SQL dynamically from the entity's public properties and
/// automatically injects the TenantId from the current tenant context.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class GenericRepository<T> : IGenericRepository<T>
    where T : AuditableTenantEntity
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly string _tableName;

    protected GenericRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext, string tableName)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _tableName = tableName;
    }

    protected ITenantContext TenantContext => _tenantContext;
    protected string TableName => _tableName;

    /// <summary>
    /// Resolves the tenant id for the current request. Throws if no tenant
    /// context is available - repositories must never return cross-tenant data.
    /// </summary>
    protected Guid RequireTenantId()
    {
        if (!_tenantContext.IsAvailable || _tenantContext.TenantId is null)
            throw new InvalidOperationException("Tenant context is not available for this operation.");
        return _tenantContext.TenantId.Value;
    }

    /// <summary>
    /// Returns a connection that participates in the supplied transaction when
    /// provided, otherwise a fresh connection owned by the caller.
    /// </summary>
    protected async Task<IDbConnection> GetConnectionAsync(IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
            return transaction.Connection!;

        var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
            await OpenAsync(connection, cancellationToken);
        return connection;
    }

    private static async Task OpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is System.Data.Common.DbConnection dbConnection)
            await dbConnection.OpenAsync(cancellationToken);
        else
            connection.Open();
    }

    /// <summary>
    /// Maps a CLR property name to a SQL column name. Override to customise
    /// column naming per entity.
    /// </summary>
    protected virtual string MapColumn(PropertyInfo property) => property.Name;

    /// <summary>
    /// The set of properties written on INSERT (excludes computed/identity columns
    /// and read-only properties that have no backing column, e.g. RefreshToken.IsExpired).
    /// </summary>
    protected virtual IEnumerable<PropertyInfo> InsertableProperties() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetSetMethod(nonPublic: true) is not null
                     && p.GetIndexParameters().Length == 0
                     && p.Name != nameof(AuditableTenantEntity.Id)
                     && p.Name != nameof(AuditableTenantEntity.RowVersion)
                     && p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() is null);

    /// <summary>
    /// The set of properties written on UPDATE (excludes identity + audit-on-create).
    /// </summary>
    protected virtual IEnumerable<PropertyInfo> UpdatableProperties() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(AuditableTenantEntity.Id)
                     && p.Name != nameof(AuditableTenantEntity.TenantId)
                     && p.Name != nameof(AuditableTenantEntity.CreatedBy)
                     && p.Name != nameof(AuditableTenantEntity.CreatedDate)
                     && p.Name != nameof(AuditableTenantEntity.RowVersion)
                     && p.Name != nameof(AuditableTenantEntity.IsDeleted)
                     && p.Name != nameof(AuditableTenantEntity.DeletedBy)
                     && p.Name != nameof(AuditableTenantEntity.DeletedDate)
                     && p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() is null);

    public async Task<T?> GetByIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM {0} WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = 0";
        var formatted = string.Format(sql, _tableName);

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(formatted, new { Id = id, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        var parser = reader.GetRowParser<T>();
        while (await ((System.Data.Common.DbDataReader)reader).ReadAsync(cancellationToken))
        {
            return parser(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var sql = $"SELECT * FROM {_tableName} WHERE TenantId = @TenantId AND IsDeleted = 0 ORDER BY CreatedDate DESC";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<T>(new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<(IReadOnlyList<T> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search = null, string? orderBy = null,
        bool ascending = true, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var offset = (page - 1) * pageSize;

        var orderClause = string.IsNullOrWhiteSpace(orderBy)
            ? "CreatedDate DESC"
            : $"{SqlSafeIdentifier(orderBy)} {(ascending ? "ASC" : "DESC")}";

        var sb = new StringBuilder();
        sb.Append("SELECT * FROM ").Append(_tableName)
          .Append(" WHERE TenantId = @TenantId AND IsDeleted = 0");
        if (!string.IsNullOrWhiteSpace(search))
            sb.Append(" AND Name LIKE @Search");
        sb.Append(" ORDER BY ").Append(orderClause)
          .Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

        sb.Append("SELECT COUNT(*) FROM ").Append(_tableName)
          .Append(" WHERE TenantId = @TenantId AND IsDeleted = 0");
        if (!string.IsNullOrWhiteSpace(search))
            sb.Append(" AND Name LIKE @Search");

        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Search", $"%{search}%");

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sb.ToString(), parameters, transaction, cancellationToken: cancellationToken));
        var items = (await multi.ReadAsync<T>()).AsList();
        var total = await multi.ReadFirstAsync<int>();
        return (items, total);
    }

    public async Task<Guid> InsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        entity.TenantId = tenantId;
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        if (entity.CreatedDate == default) entity.CreatedDate = DateTime.UtcNow;
        entity.CreatedBy ??= _tenantContext.UserId;

        var props = InsertableProperties().ToList();
        var columns = string.Join(", ", props.Select(p => $"[{MapColumn(p)}]"));
        var parameters = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {_tableName} (Id, {columns}) VALUES (@Id, {parameters}); SELECT @Id;";

        var dp = new DynamicParameters();
        foreach (var prop in props)
            dp.Add(prop.Name, prop.GetValue(entity));
        dp.Add("Id", entity.Id);

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, dp, transaction, cancellationToken: cancellationToken));
        return entity.Id;
    }

    public async Task<int> UpdateAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy ??= _tenantContext.UserId;

        var props = UpdatableProperties().ToList();
        var setClause = string.Join(", ", props.Select(p => $"[{MapColumn(p)}] = @{p.Name}"));
        var sql = $"UPDATE {_tableName} SET {setClause} WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = 0";

        var dp = new DynamicParameters();
        foreach (var prop in props)
            dp.Add(prop.Name, prop.GetValue(entity));
        dp.Add("Id", entity.Id);
        dp.Add("TenantId", tenantId);

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql, dp, transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> SoftDeleteAsync(Guid id, Guid deletedBy, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "UPDATE {0} SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedDate = @DeletedDate WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = 0";
        var formatted = string.Format(sql, _tableName);

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(formatted,
            new { Id = id, TenantId = tenantId, DeletedBy = deletedBy, DeletedDate = DateTime.UtcNow },
            transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> CountAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var sql = $"SELECT COUNT(*) FROM {_tableName} WHERE TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        return count > 0;
    }

    /// <summary>
    /// Guards against SQL injection in dynamic ORDER BY clauses by allowing only
    /// alphanumeric + underscore identifiers.
    /// </summary>
    private static string SqlSafeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return "CreatedDate";
        var sanitized = new string(identifier.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return string.IsNullOrEmpty(sanitized) ? "CreatedDate" : sanitized;
    }
}
