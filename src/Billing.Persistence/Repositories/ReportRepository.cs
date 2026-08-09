using System.Data;
using Billing.Persistence.ConnectionFactory;
using Billing.Persistence.TenantContext;
using Dapper;

namespace Billing.Persistence.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ReportRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
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

    public async Task<DashboardSummary> GetDashboardSummaryAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                -- Total Revenue: sum of all completed sale grand totals across all shops
                ISNULL((SELECT SUM(GrandTotal) FROM Sales WHERE IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS TotalSales,

                -- Total Purchases across all shops
                ISNULL((SELECT SUM(GrandTotal) FROM Purchases WHERE IsDeleted = 0 AND PurchaseDate >= @From AND PurchaseDate < @To), 0) AS TotalPurchases,

                -- Operating Expenses across all shops
                ISNULL((SELECT SUM(Amount) FROM Expenses WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To), 0) AS TotalExpenses,

                -- Cost of Goods Sold across all shops
                ISNULL((
                    SELECT SUM(si.CostPrice * si.Quantity)
                    FROM SaleItems si
                    INNER JOIN Sales s ON s.Id = si.SaleId
                    WHERE si.IsDeleted = 0
                      AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
                ), 0) AS TotalProfit,

                ISNULL((SELECT COUNT(*) FROM Sales WHERE IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS SalesCount,
                ISNULL((SELECT COUNT(*) FROM Products WHERE IsDeleted = 0), 0) AS ProductCount,
                ISNULL((SELECT COUNT(*) FROM Customers WHERE IsDeleted = 0), 0) AS CustomerCount,
                ISNULL((SELECT COUNT(*) FROM Products WHERE IsDeleted = 0 AND IsActive = 1 AND CurrentStock <= ReorderLevel), 0) AS LowStockCount;"
            : @"
            SELECT
                -- Total Revenue: sum of all completed sale grand totals
                ISNULL((SELECT SUM(GrandTotal) FROM Sales WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS TotalSales,

                -- Total Purchases (purchase orders placed with suppliers)
                ISNULL((SELECT SUM(GrandTotal) FROM Purchases WHERE TenantId = @TenantId AND IsDeleted = 0 AND PurchaseDate >= @From AND PurchaseDate < @To), 0) AS TotalPurchases,

                -- Operating Expenses
                ISNULL((SELECT SUM(Amount) FROM Expenses WHERE TenantId = @TenantId AND IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To), 0) AS TotalExpenses,

                -- Cost of Goods Sold: actual cost price of items sold (from SaleItems)
                ISNULL((
                    SELECT SUM(si.CostPrice * si.Quantity)
                    FROM SaleItems si
                    INNER JOIN Sales s ON s.Id = si.SaleId AND s.TenantId = si.TenantId
                    WHERE si.TenantId = @TenantId AND si.IsDeleted = 0
                      AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
                ), 0) AS TotalProfit,

                ISNULL((SELECT COUNT(*) FROM Sales WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS SalesCount,
                ISNULL((SELECT COUNT(*) FROM Products WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS ProductCount,
                ISNULL((SELECT COUNT(*) FROM Customers WHERE TenantId = @TenantId AND IsDeleted = 0), 0) AS CustomerCount,
                ISNULL((SELECT COUNT(*) FROM Products WHERE TenantId = @TenantId AND IsDeleted = 0 AND IsActive = 1 AND CurrentStock <= ReorderLevel), 0) AS LowStockCount;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var summary = await connection.QuerySingleAsync<DashboardSummary>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));

        // Net Profit = Revenue - Cost of Goods Sold - Operating Expenses
        var cogs = summary.TotalProfit;
        var netProfit = summary.TotalSales - cogs - summary.TotalExpenses;
        return summary with { TotalProfit = netProfit };
    }

    public async Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime from, DateTime to, int top = 10, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT TOP (@Top)
                si.ProductId,
                si.ProductName,
                SUM(si.Quantity) AS QuantitySold,
                SUM(si.LineTotal) AS Revenue
            FROM SaleItems si
            INNER JOIN Sales s ON s.Id = si.SaleId
            WHERE si.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY si.ProductId, si.ProductName
            ORDER BY Revenue DESC;"
            : @"
            SELECT TOP (@Top)
                si.ProductId,
                si.ProductName,
                SUM(si.Quantity) AS QuantitySold,
                SUM(si.LineTotal) AS Revenue
            FROM SaleItems si
            INNER JOIN Sales s ON s.Id = si.SaleId AND s.TenantId = si.TenantId
            WHERE si.TenantId = @TenantId AND si.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY si.ProductId, si.ProductName
            ORDER BY Revenue DESC;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<TopProductRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to, Top = top }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                CAST(SaleDate AS DATE) AS Date,
                SUM(GrandTotal) AS TotalSales,
                COUNT(*) AS SalesCount
            FROM Sales
            WHERE IsDeleted = 0 AND Status = 3
              AND SaleDate >= @From AND SaleDate < @To
            GROUP BY CAST(SaleDate AS DATE)
            ORDER BY Date;"
            : @"
            SELECT
                CAST(SaleDate AS DATE) AS Date,
                SUM(GrandTotal) AS TotalSales,
                COUNT(*) AS SalesCount
            FROM Sales
            WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 3
              AND SaleDate >= @From AND SaleDate < @To
            GROUP BY CAST(SaleDate AS DATE)
            ORDER BY Date;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<DailySalesRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<ProfitLossRow> GetProfitLossAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                ISNULL((SELECT SUM(GrandTotal) FROM Sales WHERE IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS Revenue,
                ISNULL((SELECT SUM(si.Quantity * si.CostPrice) FROM SaleItems si INNER JOIN Sales s ON s.Id = si.SaleId WHERE si.IsDeleted = 0 AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To), 0) AS CostOfGoods,
                ISNULL((SELECT SUM(Amount) FROM Expenses WHERE IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To), 0) AS Expenses,
                CAST(0 AS DECIMAL(18,2)) AS GrossProfit,
                CAST(0 AS DECIMAL(18,2)) AS NetProfit;"
            : @"
            SELECT
                ISNULL((SELECT SUM(GrandTotal) FROM Sales WHERE TenantId = @TenantId AND IsDeleted = 0 AND Status = 3 AND SaleDate >= @From AND SaleDate < @To), 0) AS Revenue,
                ISNULL((SELECT SUM(si.Quantity * si.CostPrice) FROM SaleItems si INNER JOIN Sales s ON s.Id = si.SaleId WHERE si.TenantId = @TenantId AND si.IsDeleted = 0 AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To), 0) AS CostOfGoods,
                ISNULL((SELECT SUM(Amount) FROM Expenses WHERE TenantId = @TenantId AND IsDeleted = 0 AND ExpenseDate >= @From AND ExpenseDate < @To), 0) AS Expenses,
                CAST(0 AS DECIMAL(18,2)) AS GrossProfit,
                CAST(0 AS DECIMAL(18,2)) AS NetProfit;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var row = await connection.QuerySingleAsync<ProfitLossRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));
        var gross = row.Revenue - row.CostOfGoods;
        return row with { GrossProfit = gross, NetProfit = gross - row.Expenses };
    }

    public async Task<IReadOnlyList<SalesReportRow>> GetSalesReportAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                s.SaleDate,
                s.InvoiceNumber,
                c.Name AS CustomerName,
                s.SubTotal,
                s.TaxAmount,
                s.GrandTotal,
                CAST(s.Status AS NVARCHAR(20)) AS Status,
                CAST(s.PaymentStatus AS NVARCHAR(20)) AS PaymentStatus
            FROM Sales s
            LEFT JOIN Customers c ON c.Id = s.CustomerId
            WHERE s.IsDeleted = 0
              AND s.SaleDate >= @From AND s.SaleDate < @To
            ORDER BY s.SaleDate DESC;"
            : @"
            SELECT
                s.SaleDate,
                s.InvoiceNumber,
                c.Name AS CustomerName,
                s.SubTotal,
                s.TaxAmount,
                s.GrandTotal,
                CAST(s.Status AS NVARCHAR(20)) AS Status,
                CAST(s.PaymentStatus AS NVARCHAR(20)) AS PaymentStatus
            FROM Sales s
            LEFT JOIN Customers c ON c.Id = s.CustomerId AND c.TenantId = s.TenantId
            WHERE s.TenantId = @TenantId AND s.IsDeleted = 0
              AND s.SaleDate >= @From AND s.SaleDate < @To
            ORDER BY s.SaleDate DESC;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<SalesReportRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<GstRateBreakdownRow>> GetGstReportAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                si.TaxRate,
                SUM(si.LineTotal - si.TaxAmount) AS TaxableAmount,
                SUM(si.TaxAmount) AS TaxAmount,
                COUNT(DISTINCT s.Id) AS InvoiceCount
            FROM SaleItems si
            INNER JOIN Sales s ON s.Id = si.SaleId
            WHERE si.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY si.TaxRate
            ORDER BY si.TaxRate;"
            : @"
            SELECT
                si.TaxRate,
                SUM(si.LineTotal - si.TaxAmount) AS TaxableAmount,
                SUM(si.TaxAmount) AS TaxAmount,
                COUNT(DISTINCT s.Id) AS InvoiceCount
            FROM SaleItems si
            INNER JOIN Sales s ON s.Id = si.SaleId AND s.TenantId = si.TenantId
            WHERE si.TenantId = @TenantId AND si.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY si.TaxRate
            ORDER BY si.TaxRate;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<GstRateBreakdownRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<PaymentMethodSummaryRow>> GetPaymentMethodSummaryAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                CAST(p.Method AS NVARCHAR(20)) AS PaymentMethod,
                SUM(p.Amount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM Payments p
            INNER JOIN Sales s ON s.Id = p.SaleId
            WHERE p.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY CAST(p.Method AS NVARCHAR(20))
            ORDER BY TotalAmount DESC;"
            : @"
            SELECT
                CAST(p.Method AS NVARCHAR(20)) AS PaymentMethod,
                SUM(p.Amount) AS TotalAmount,
                COUNT(*) AS TransactionCount
            FROM Payments p
            INNER JOIN Sales s ON s.Id = p.SaleId AND s.TenantId = p.TenantId
            WHERE p.TenantId = @TenantId AND p.IsDeleted = 0
              AND s.Status = 3 AND s.SaleDate >= @From AND s.SaleDate < @To
            GROUP BY CAST(p.Method AS NVARCHAR(20))
            ORDER BY TotalAmount DESC;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<PaymentMethodSummaryRow>(
            new CommandDefinition(sql, new { TenantId = tenantId, From = from, To = to }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<InventoryValuationRow>> GetInventoryValuationAsync(bool isGlobalAdmin = false, CancellationToken cancellationToken = default)
    {
        var tenantId = isGlobalAdmin ? Guid.Empty : RequireTenantId();
        var sql = isGlobalAdmin ? @"
            SELECT
                p.Id AS ProductId,
                p.Name AS ProductName,
                p.Sku,
                p.CurrentStock,
                p.CostPrice,
                (p.CurrentStock * p.CostPrice) AS StockValue
            FROM Products p
            WHERE p.IsDeleted = 0 AND p.IsActive = 1
            ORDER BY p.Name;"
            : @"
            SELECT
                p.Id AS ProductId,
                p.Name AS ProductName,
                p.Sku,
                p.CurrentStock,
                p.CostPrice,
                (p.CurrentStock * p.CostPrice) AS StockValue
            FROM Products p
            WHERE p.TenantId = @TenantId AND p.IsDeleted = 0 AND p.IsActive = 1
            ORDER BY p.Name;";

        using var connection = _factory.CreateConnection();
        if (connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
        var result = await connection.QueryAsync<InventoryValuationRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
