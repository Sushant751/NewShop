using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Customers") { }

    public async Task<Customer?> GetByEmailAsync(string email, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Customers WHERE TenantId = @TenantId AND Email = @Email AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { TenantId = tenantId, Email = email }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Customers WHERE TenantId = @TenantId AND Phone = @Phone AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { TenantId = tenantId, Phone = phone }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string term, int limit = 20, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT TOP (@Limit) * FROM Customers
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1
              AND (Name LIKE @Term OR Phone LIKE @Term OR Email LIKE @Term)
            ORDER BY Name";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Customer>(
            new CommandDefinition(sql, new { TenantId = tenantId, Term = $"%{term}%", Limit = limit }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }
}

public sealed class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Suppliers") { }

    public async Task<IReadOnlyList<Supplier>> SearchAsync(string term, int limit = 20, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT TOP (@Limit) * FROM Suppliers
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1
              AND (Name LIKE @Term OR Phone LIKE @Term OR Email LIKE @Term)
            ORDER BY Name";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Supplier>(
            new CommandDefinition(sql, new { TenantId = tenantId, Term = $"%{term}%", Limit = limit }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
