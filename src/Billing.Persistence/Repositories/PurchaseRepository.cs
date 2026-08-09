using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class PurchaseRepository : GenericRepository<Purchase>, IPurchaseRepository
{
    public PurchaseRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Purchases") { }

    public async Task<Purchase?> GetWithItemsAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT * FROM Purchases WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = 0;
            SELECT * FROM PurchaseItems WHERE PurchaseId = @Id AND IsDeleted = 0;";

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));

        var purchase = await multi.ReadFirstOrDefaultAsync<Purchase>();
        if (purchase is null) return null;
        purchase.Items = (await multi.ReadAsync<PurchaseItem>()).AsList();
        return purchase;
    }

    public async Task<string> GeneratePurchaseNumberAsync(IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "EXEC sp_GeneratePurchaseNumber @TenantId = @TenantId";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreatePurchaseAsync(Purchase purchase, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (purchase.Id == Guid.Empty) purchase.Id = Guid.NewGuid();
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        if (purchase.ShopId == Guid.Empty) purchase.ShopId = null;
        purchase.TenantId = tenantId;
        purchase.CreatedDate = DateTime.UtcNow;
        purchase.CreatedBy ??= TenantContext.UserId;

        const string purchaseSql = @"
            INSERT INTO Purchases
                (Id, TenantId, PurchaseNumber, ShopId, SupplierId, PurchaseDate, Status,
                 SubTotal, DiscountAmount, TaxAmount, GrandTotal, PaidAmount, BalanceDue,
                 Notes, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @PurchaseNumber, @ShopId, @SupplierId, @PurchaseDate, @Status,
                 @SubTotal, @DiscountAmount, @TaxAmount, @GrandTotal, @PaidAmount, @BalanceDue,
                 @Notes, @CreatedDate, @CreatedBy, 0);";

        const string itemSql = @"
            INSERT INTO PurchaseItems
                (Id, TenantId, PurchaseId, ProductId, ProductName, Quantity, UnitCost,
                 TaxRate, TaxAmount, LineTotal, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @PurchaseId, @ProductId, @ProductName, @Quantity, @UnitCost,
                 @TaxRate, @TaxAmount, @LineTotal, @CreatedDate, @CreatedBy, 0);

            UPDATE Products
            SET CurrentStock = CurrentStock + @Quantity,
                CostPrice = @UnitCost
            WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0;";

        await connection.ExecuteAsync(new CommandDefinition(purchaseSql, purchase, transaction, cancellationToken: cancellationToken));

        foreach (var item in purchase.Items)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.TenantId = tenantId;
            item.PurchaseId = purchase.Id;
            item.CreatedDate = purchase.CreatedDate;
            item.CreatedBy = purchase.CreatedBy;
            await connection.ExecuteAsync(new CommandDefinition(itemSql, item, transaction, cancellationToken: cancellationToken));
        }

        return purchase.Id;
    }

    public async Task<string?> GetSupplierNameAsync(Guid supplierId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = "SELECT Name FROM dbo.Suppliers WHERE Id = @SupplierId AND TenantId = @TenantId AND IsDeleted = 0";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { SupplierId = supplierId, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }
}
