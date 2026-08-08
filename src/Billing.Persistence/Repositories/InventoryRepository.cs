using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public InventoryRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    private Guid RequireTenantId() =>
        _tenantContext.IsAvailable ? _tenantContext.TenantId!.Value
            : throw new InvalidOperationException("Tenant context is not available.");

    private static async Task<IDbConnection> GetConnectionAsync(IDbConnectionFactory factory, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null) return transaction.Connection!;
        var connection = factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        return connection;
    }

    public async Task<decimal> GetStockOnHandAsync(Guid productId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT ISNULL(CurrentStock, 0) FROM Products WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { ProductId = productId, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid productId, DateTime? from = null, DateTime? to = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT * FROM StockMovements
            WHERE TenantId = @TenantId AND ProductId = @ProductId
              AND (@From IS NULL OR CreatedDate >= @From)
              AND (@To IS NULL OR CreatedDate < @To)
            ORDER BY CreatedDate DESC";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        var result = await connection.QueryAsync<StockMovement>(
            new CommandDefinition(sql, new { TenantId = tenantId, ProductId = productId, From = from, To = to }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task RecordMovementAsync(StockMovement movement, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (movement.Id == Guid.Empty) movement.Id = Guid.NewGuid();
        movement.TenantId = tenantId;
        movement.CreatedDate = DateTime.UtcNow;
        movement.CreatedBy ??= _tenantContext.UserId;

        const string sql = @"
            INSERT INTO StockMovements
                (Id, TenantId, ProductId, ShopId, MovementType, Quantity, UnitCost,
                 Reference, ReferenceId, Notes, BalanceAfter, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @ProductId, @ShopId, @MovementType, @Quantity, @UnitCost,
                 @Reference, @ReferenceId, @Notes, @BalanceAfter, @CreatedDate, @CreatedBy, 0);";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, movement, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Inventory>> GetLowStockAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT i.* FROM Inventory i
            INNER JOIN Products p ON p.Id = i.ProductId AND p.TenantId = i.TenantId
            WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
              AND i.QuantityOnHand <= i.ReorderLevel";
        var connection = await GetConnectionAsync(_factory, transaction, cancellationToken);
        var result = await connection.QueryAsync<Inventory>(
            new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
