namespace Billing.Domain.Entities;

public sealed class Category : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Brand : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Unit : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Product : Base.AuditableTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsTaxable { get; set; } = true;
    public decimal ReorderLevel { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TrackInventory { get; set; } = true;
    public bool AllowSaleWithoutStock { get; set; } = false;
}

public sealed class Inventory : Base.AuditableTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? ShopId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class StockMovement : Base.AuditableTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? ShopId { get; set; }
    public int MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Reference { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public decimal BalanceAfter { get; set; }
}
