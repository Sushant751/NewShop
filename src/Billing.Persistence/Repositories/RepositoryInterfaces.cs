using Billing.Domain.Entities;

namespace Billing.Persistence.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(decimal? thresholdOverride = null, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchAsync(string term, int limit = 20, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> AdjustStockAsync(Guid productId, decimal delta, int movementType, Guid? referenceId, string? reference, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Customer?> GetByPhoneAsync(string phone, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string term, int limit = 20, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface ISupplierRepository : IGenericRepository<Supplier>
{
    Task<IReadOnlyList<Supplier>> SearchAsync(string term, int limit = 20, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface ISalesRepository : IGenericRepository<Sale>
{
    Task<Sale?> GetWithItemsAsync(Guid id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumberAsync(System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<Guid> CreateSaleAsync(Sale sale, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<int> CancelSaleAsync(Guid saleId, Guid cancelledBy, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime from, DateTime to, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSalesAsync(DateTime from, DateTime to, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    new Task<IReadOnlyList<Sale>> GetAllAsync(System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface IPurchaseRepository : IGenericRepository<Purchase>
{
    Task<Purchase?> GetWithItemsAsync(Guid id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<string> GeneratePurchaseNumberAsync(System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<Guid> CreatePurchaseAsync(Purchase purchase, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<string?> GetSupplierNameAsync(Guid supplierId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface IInventoryRepository
{
    Task<decimal> GetStockOnHandAsync(Guid productId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid productId, DateTime? from = null, DateTime? to = null, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task RecordMovementAsync(StockMovement movement, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetLowStockAsync(System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface ITenantRepository : IGlobalRepository<Tenant>
{
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<int> UpdateStatusAsync(Guid tenantId, Shared.Enums.TenantStatus status, Guid updatedBy, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailGlobalAsync(string email, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameAsync(string userName, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Guid> GetRoleIdByNameAsync(string roleName, Guid tenantId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, string? ip, string? deviceInfo, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    /// <summary>Returns all users across every tenant, each with their tenant name. GlobalAdmin only.</summary>
    Task<IReadOnlyList<(User User, string TenantName)>> GetAllGlobalAsync(System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid tokenId, string? replacedByToken, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task LogAsync(AuditLog log, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public interface IReportRepository
{
    Task<DashboardSummary> GetDashboardSummaryAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime from, DateTime to, int top = 10, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<ProfitLossRow> GetProfitLossAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesReportRow>> GetSalesReportAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GstRateBreakdownRow>> GetGstReportAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethodSummaryRow>> GetPaymentMethodSummaryAsync(DateTime from, DateTime to, bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryValuationRow>> GetInventoryValuationAsync(bool isGlobalAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopMetricsRow>> GetShopMetricsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public sealed record DashboardSummary(
    decimal TotalSales, decimal TotalPurchases, decimal TotalExpenses,
    decimal TotalProfit, int SalesCount, int ProductCount, int CustomerCount,
    int LowStockCount, int TotalShopsCount = 0, int TotalUsersCount = 0,
    int TotalCancelledBillsCount = 0, decimal TotalCancelledAmount = 0);

public sealed record ShopMetricsRow(
    Guid TenantId, string TenantName, string TenantSlug, string? Plan, string Status,
    int UserCount, int ProductCount, int TotalBillsGenerated, int PaidBillsCount,
    int CancelledBillsCount, decimal TotalRevenue, decimal CancelledAmount,
    decimal OutstandingAmount, DateTime CreatedDate);

public sealed record TopProductRow(Guid ProductId, string ProductName, decimal QuantitySold, decimal Revenue);
public sealed record DailySalesRow(DateTime Date, decimal TotalSales, int SalesCount);
public sealed record ProfitLossRow(decimal Revenue, decimal CostOfGoods, decimal Expenses, decimal GrossProfit, decimal NetProfit);
public sealed record SalesReportRow(DateTime SaleDate, string InvoiceNumber, string? CustomerName, decimal SubTotal, decimal TaxAmount, decimal GrandTotal, string Status, string PaymentStatus);
public sealed record GstRateBreakdownRow(decimal TaxRate, decimal TaxableAmount, decimal TaxAmount, int InvoiceCount);
public sealed record PaymentMethodSummaryRow(string PaymentMethod, decimal TotalAmount, int TransactionCount);
public sealed record InventoryValuationRow(Guid ProductId, string ProductName, string? Sku, decimal CurrentStock, decimal CostPrice, decimal StockValue);
