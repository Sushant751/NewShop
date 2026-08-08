using System.Data;
using Billing.Domain.Entities.Base;

namespace Billing.Persistence.Repositories;

/// <summary>
/// Generic repository contract for tenant-scoped entities. All operations are
/// automatically filtered by the current tenant context.
/// </summary>
/// <typeparam name="T">Entity type derived from <see cref="AuditableTenantEntity"/>.</typeparam>
public interface IGenericRepository<T> where T : AuditableTenantEntity
{
    Task<T?> GetByIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, string? orderBy = null, bool ascending = true, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> SoftDeleteAsync(Guid id, Guid deletedBy, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository contract for global (non-tenant-scoped) entities.
/// </summary>
public interface IGlobalRepository<T> where T : AuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Guid> InsertAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(T entity, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> SoftDeleteAsync(Guid id, Guid deletedBy, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
