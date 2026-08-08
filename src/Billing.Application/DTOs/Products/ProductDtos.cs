namespace Billing.Application.DTOs.Products;

public sealed record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid? BrandId { get; init; }
    public string? BrandName { get; init; }
    public Guid? UnitId { get; init; }
    public string? UnitName { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal TaxRate { get; init; }
    public bool IsTaxable { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal CurrentStock { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public bool TrackInventory { get; init; }
}

public sealed record CreateProductRequest(
    string Name, string? Description, string? Sku, string? Barcode,
    Guid? CategoryId, Guid? BrandId, Guid? UnitId,
    decimal CostPrice, decimal SellingPrice, decimal TaxRate, bool IsTaxable,
    decimal ReorderLevel, decimal OpeningStock, string? ImageUrl,
    bool TrackInventory, bool AllowSaleWithoutStock);

public sealed record UpdateProductRequest(
    string Name, string? Description, string? Sku, string? Barcode,
    Guid? CategoryId, Guid? BrandId, Guid? UnitId,
    decimal CostPrice, decimal SellingPrice, decimal TaxRate, bool IsTaxable,
    decimal ReorderLevel, string? ImageUrl, bool IsActive,
    bool TrackInventory, bool AllowSaleWithoutStock);

public sealed record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentCategoryId, bool IsActive);
public sealed record CreateCategoryRequest(string Name, string? Description, Guid? ParentCategoryId);

public sealed record BrandDto(Guid Id, string Name, string? Description, bool IsActive);
public sealed record CreateBrandRequest(string Name, string? Description);

public sealed record UnitDto(Guid Id, string Name, string Code, string? Description, bool IsActive);
public sealed record CreateUnitRequest(string Name, string Code, string? Description);
