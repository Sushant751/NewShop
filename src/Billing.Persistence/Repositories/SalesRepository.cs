using System.Data;
using Billing.Domain.Entities;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class SalesRepository : GenericRepository<Sale>, ISalesRepository
{
    public SalesRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory, tenantContext, "Sales") { }

    public async Task<Sale?> GetWithItemsAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT s.*, c.Name AS CustomerName
            FROM Sales s
            LEFT JOIN Customers c ON c.Id = s.CustomerId AND c.TenantId = s.TenantId
            WHERE s.Id = @Id AND s.TenantId = @TenantId AND s.IsDeleted = 0;
            SELECT * FROM SaleItems WHERE SaleId = @Id AND IsDeleted = 0;
            SELECT * FROM Payments WHERE SaleId = @Id AND IsDeleted = 0;";

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));

        var sale = await multi.ReadFirstOrDefaultAsync<Sale>();
        if (sale is null) return null;
        sale.Items = (await multi.ReadAsync<SaleItem>()).AsList();
        sale.Payments = (await multi.ReadAsync<Payment>()).AsList();
        return sale;
    }

    public async Task<string> GenerateInvoiceNumberAsync(IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        // Use a stored procedure for atomic sequence generation.
        const string sql = "EXEC sp_GenerateInvoiceNumber @TenantId = @TenantId";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateSaleAsync(Sale sale, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        if (sale.Id == Guid.Empty) sale.Id = Guid.NewGuid();
        sale.TenantId = tenantId;
        sale.CreatedDate = DateTime.UtcNow;
        sale.CreatedBy ??= TenantContext.UserId;

        const string saleSql = @"
            INSERT INTO Sales
                (Id, TenantId, InvoiceNumber, ShopId, CustomerId, CashierId, SaleDate, Status,
                 PaymentStatus, SubTotal, DiscountAmount, TaxAmount, RoundOff, GrandTotal,
                 PaidAmount, BalanceDue, Notes, CouponCode, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @InvoiceNumber, @ShopId, @CustomerId, @CashierId, @SaleDate, @Status,
                 @PaymentStatus, @SubTotal, @DiscountAmount, @TaxAmount, @RoundOff, @GrandTotal,
                 @PaidAmount, @BalanceDue, @Notes, @CouponCode, @CreatedDate, @CreatedBy, 0);";

        const string itemSql = @"
            INSERT INTO SaleItems
                (Id, TenantId, SaleId, ProductId, ProductName, Quantity, UnitPrice, CostPrice,
                 DiscountAmount, TaxRate, TaxAmount, LineTotal, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @SaleId, @ProductId, @ProductName, @Quantity, @UnitPrice, @CostPrice,
                 @DiscountAmount, @TaxRate, @TaxAmount, @LineTotal, @CreatedDate, @CreatedBy, 0);

            UPDATE Products
            SET CurrentStock = CurrentStock - @Quantity
            WHERE Id = @ProductId AND TenantId = @TenantId AND IsDeleted = 0;
            
            INSERT INTO StockMovements
                (Id, TenantId, ProductId, MovementType, Quantity, ReferenceId, Notes, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (NEWID(), @TenantId, @ProductId, 2, @Quantity, @SaleId, 'Sale ' + @InvoiceNumber, @CreatedDate, @CreatedBy, 0);";

        const string paymentSql = @"
            INSERT INTO Payments
                (Id, TenantId, SaleId, Method, Amount, Reference, Notes, PaidAt, CreatedDate, CreatedBy, IsDeleted)
            VALUES
                (@Id, @TenantId, @SaleId, @Method, @Amount, @Reference, @Notes, @PaidAt, @CreatedDate, @CreatedBy, 0);";

        var connection = await GetConnectionAsync(transaction, cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(saleSql, sale, transaction, cancellationToken: cancellationToken));

        foreach (var item in sale.Items)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.TenantId = tenantId;
            item.SaleId = sale.Id;
            item.CreatedDate = sale.CreatedDate;
            item.CreatedBy = sale.CreatedBy;
            await connection.ExecuteAsync(new CommandDefinition(itemSql, new
            {
                item.Id,
                item.TenantId,
                item.SaleId,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.CostPrice,
                item.DiscountAmount,
                item.TaxRate,
                item.TaxAmount,
                item.LineTotal,
                item.CreatedDate,
                item.CreatedBy,
                sale.InvoiceNumber
            }, transaction, cancellationToken: cancellationToken));
        }

        foreach (var payment in sale.Payments)
        {
            if (payment.Id == Guid.Empty) payment.Id = Guid.NewGuid();
            payment.TenantId = tenantId;
            payment.SaleId = sale.Id;
            payment.CreatedDate = sale.CreatedDate;
            payment.CreatedBy = sale.CreatedBy;
            await connection.ExecuteAsync(new CommandDefinition(paymentSql, payment, transaction, cancellationToken: cancellationToken));
        }

        return sale.Id;
    }

    public async Task<int> CancelSaleAsync(Guid saleId, Guid cancelledBy, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            UPDATE Sales
            SET Status = 4, UpdatedBy = @CancelledBy, UpdatedDate = @Now
            WHERE Id = @SaleId AND TenantId = @TenantId AND IsDeleted = 0 AND Status = 3;

            -- Restore stock for cancelled sale
            UPDATE p
            SET p.CurrentStock = p.CurrentStock + si.Quantity
            FROM Products p
            INNER JOIN SaleItems si ON si.ProductId = p.Id AND si.SaleId = @SaleId
            WHERE p.TenantId = @TenantId AND p.IsDeleted = 0;";

        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteAsync(new CommandDefinition(sql,
            new { SaleId = saleId, TenantId = tenantId, CancelledBy = cancelledBy, Now = DateTime.UtcNow },
            transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime from, DateTime to, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT s.*, c.Name AS CustomerName
            FROM Sales s
            LEFT JOIN Customers c ON c.Id = s.CustomerId AND c.TenantId = s.TenantId
            WHERE s.TenantId = @TenantId AND s.IsDeleted = 0
              AND s.SaleDate >= @From AND s.SaleDate < @To
            ORDER BY s.SaleDate DESC";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Sale>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public new async Task<IReadOnlyList<Sale>> GetAllAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT s.*, c.Name AS CustomerName
            FROM Sales s
            LEFT JOIN Customers c ON c.Id = s.CustomerId AND c.TenantId = s.TenantId
            WHERE s.TenantId = @TenantId AND s.IsDeleted = 0
            ORDER BY s.CreatedDate DESC";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        var result = await connection.QueryAsync<Sale>(new CommandDefinition(sql, new { TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime from, DateTime to, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        const string sql = @"
            SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 3
              AND SaleDate >= @From AND SaleDate < @To";
        var connection = await GetConnectionAsync(transaction, cancellationToken);
        return await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, transaction, cancellationToken: cancellationToken));
    }
}
