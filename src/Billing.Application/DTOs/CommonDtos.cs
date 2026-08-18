using Billing.Shared.Enums;

namespace Billing.Application.DTOs;

public sealed record CustomerDto(
    Guid Id, string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PostalCode,
    string? TaxNumber, decimal CurrentBalance, decimal CreditLimit, bool IsActive);

public sealed record CreateCustomerRequest(
    string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PostalCode,
    string? TaxNumber, decimal OpeningBalance, decimal CreditLimit);

public sealed record UpdateCustomerRequest(
    string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PostalCode,
    string? TaxNumber, decimal CreditLimit, bool IsActive);

public sealed record SupplierDto(
    Guid Id, string Name, string? ContactPerson, string? Email, string? Phone,
    string? Address, string? City, string? State, string? Country, string? PostalCode,
    string? TaxNumber, decimal CurrentBalance, bool IsActive);

public sealed record CreateSupplierRequest(
    string Name, string? ContactPerson, string? Email, string? Phone,
    string? Address, string? City, string? State, string? Country,
    string? PostalCode, string? TaxNumber, decimal OpeningBalance);

public sealed record PurchaseItemDto(
    Guid Id, Guid ProductId, string ProductName, decimal Quantity,
    decimal UnitCost, decimal TaxRate, decimal TaxAmount, decimal LineTotal);

public sealed record PurchaseDto
{
    public Guid Id { get; init; }
    public string PurchaseNumber { get; init; } = string.Empty;
    public Guid? ShopId { get; init; }
    public Guid SupplierId { get; init; }
    public string? SupplierName { get; init; }
    public DateTime PurchaseDate { get; init; }
    public PurchaseStatus Status { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal BalanceDue { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PurchaseItemDto> Items { get; init; } = new List<PurchaseItemDto>();
}

public sealed record PurchaseItemRequest(Guid ProductId, decimal Quantity, decimal UnitCost, decimal TaxRate);

public sealed record CreatePurchaseRequest(
    Guid SupplierId, Guid? ShopId, List<PurchaseItemRequest> Items,
    decimal DiscountAmount, decimal PaidAmount, string? Notes);

public sealed record ExpenseDto(
    Guid Id, string Title, Guid? CategoryId, decimal Amount, DateTime ExpenseDate,
    PaymentMethod PaymentMethod, string? Reference, string? Notes);

public sealed record CreateExpenseRequest(
    string Title, Guid? CategoryId, decimal Amount, DateTime ExpenseDate,
    PaymentMethod PaymentMethod, string? Reference, string? Notes);

public sealed record ShopMetricsDto(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string? Plan,
    string Status,
    int UserCount,
    int ProductCount,
    int TotalBillsGenerated,
    int PaidBillsCount,
    int CancelledBillsCount,
    decimal TotalRevenue,
    decimal CancelledAmount,
    decimal OutstandingAmount,
    DateTime CreatedDate);

public sealed record DashboardDto(
    decimal TotalSales, decimal TotalPurchases, decimal TotalExpenses,
    decimal TotalProfit, int SalesCount, int ProductCount, int CustomerCount,
    int LowStockCount, IReadOnlyList<TopProductDto> TopProducts, IReadOnlyList<DailySalesDto> DailySales,
    int TotalShopsCount = 0, int TotalUsersCount = 0, int TotalCancelledBillsCount = 0,
    decimal TotalCancelledAmount = 0, decimal TotalDiscountAmount = 0, IReadOnlyList<ShopMetricsDto>? ShopMetrics = null);

public sealed record TopProductDto(Guid ProductId, string ProductName, decimal QuantitySold, decimal Revenue);
public sealed record DailySalesDto(DateTime Date, decimal TotalSales, int SalesCount);
public sealed record ProfitLossDto(decimal Revenue, decimal CostOfGoods, decimal Expenses, decimal DiscountAmount, decimal GrossProfit, decimal NetProfit);

public sealed record SalesReportDto(DateTime SaleDate, string InvoiceNumber, string? CustomerName, decimal SubTotal, decimal DiscountAmount, decimal TaxAmount, decimal GrandTotal, string Status, string PaymentStatus);
public sealed record GstReportDto(IReadOnlyList<GstRateBreakdownDto> RateBreakdown, decimal TotalTaxableAmount, decimal TotalTaxAmount, int TotalInvoices);
public sealed record GstRateBreakdownDto(decimal TaxRate, decimal TaxableAmount, decimal TaxAmount, int InvoiceCount);
public sealed record PaymentMethodSummaryDto(string PaymentMethod, decimal TotalAmount, int TransactionCount);
public sealed record InventoryValuationDto(Guid ProductId, string ProductName, string? Sku, decimal CurrentStock, decimal CostPrice, decimal StockValue);
public sealed record InventoryValuationSummaryDto(IReadOnlyList<InventoryValuationDto> Items, decimal TotalStockValue, int ProductCount);
public sealed record SalesReportSummaryDto(IReadOnlyList<SalesReportDto> Sales, decimal TotalSubTotal, decimal TotalDiscountAmount, decimal TotalTax, decimal TotalGrandTotal, int TotalCount);
public sealed record PaymentSummaryDto(IReadOnlyList<PaymentMethodSummaryDto> Methods, decimal TotalAmount, int TotalTransactions);
public sealed record ReportsDashboardDto(
    ProfitLossDto ProfitLoss,
    SalesReportSummaryDto SalesSummary,
    PaymentSummaryDto PaymentSummary,
    GstReportDto GstReport,
    InventoryValuationSummaryDto InventoryValuation);
