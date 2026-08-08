using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Products") { }

    public async Task<Product?> GetBySkuAsync(string sku, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Products WHERE TenantId = @TenantId AND Sku = @Sku AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { TenantId = tenantId, Sku = sku }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT * FROM Products WHERE TenantId = @TenantId AND Barcode = @Barcode AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { TenantId = tenantId, Barcode = barcode }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(decimal? thresholdOverride = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT * FROM Products
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1
              AND CurrentStock <= ReorderLevel
            ORDER BY CurrentStock ASC";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, int limit = 20, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT TOP (@Limit) * FROM Products
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1
              AND (Name LIKE @Term OR Sku LIKE @Term OR Barcode LIKE @Term)
            ORDER BY Name";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { TenantId = tenantId, Term = $"%{term}%", Limit = limit }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<bool> AdjustStockAsync(Guid productId, decimal delta, int movementType, Guid? referenceId, string? reference, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            UPDATE Products
            SET CurrentStock = CurrentStock + @Delta,
                UpdatedDate = @Now
            OUTPUT INSERTED.CurrentStock
            WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0;

            INSERT INTO StockMovements
                (Id, TenantId, ProductId, MovementType, Quantity, UnitCost, Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, CreatedBy)
            VALUES
                (NEWID(), @TenantId, @ProductId, @MovementType, @Delta, 0, @Reference, @ReferenceId, NULL, 0, @Now, @CreatedBy);";

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var newBalance = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql,
                new
                {
                    Delta = delta,
                    ProductId = productId,
                    TenantId = tenantId,
                    MovementType = movementType,
                    Reference = reference,
                    ReferenceId = referenceId,
                    Now = DateTime.UtcNow,
                    CreatedBy = TenantContext.UserId
                },
                transaction, cancellationToken: cancellationToken));
        return true;
    }
}
