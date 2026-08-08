using Billing.Shared.Enums;

namespace Billing.Domain.Entities;

public sealed class Customer : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Supplier : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Sale : Base.AuditableTenantEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? ShopId { get; set; }
    public Guid? CustomerId { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? CustomerName { get; set; }
    public Guid CashierId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public SaleStatus Status { get; set; } = SaleStatus.Draft;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? CouponCode { get; set; }
    public List<SaleItem> Items { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}

public sealed class SaleItem : Base.AuditableTenantEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class Purchase : Base.AuditableTenantEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid? ShopId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseItem> Items { get; set; } = new();
}

public sealed class PurchaseItem : Base.AuditableTenantEntity
{
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class Payment : Base.AuditableTenantEntity
{
    public Guid SaleId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}

public sealed class Expense : Base.AuditableTenantEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class Tax : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsInclusive { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Discount : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal Percentage { get; set; }
    public decimal? FlatAmount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}
