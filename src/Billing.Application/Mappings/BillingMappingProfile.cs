using AutoMapper;
using Billing.Application.DTOs;
using Billing.Application.DTOs.Auth;
using Billing.Application.DTOs.Products;
using Billing.Application.DTOs.Sales;
using Billing.Domain.Entities;

namespace Billing.Application.Mappings;

public sealed class BillingMappingProfile : Profile
{
    public BillingMappingProfile()
    {
        // Products
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductRequest, Product>()
            .ForMember(d => d.CurrentStock, o => o.MapFrom(s => s.OpeningStock))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore());
        CreateMap<UpdateProductRequest, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.OpeningStock, o => o.Ignore())
            .ForMember(d => d.CurrentStock, o => o.Ignore());

        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore());

        CreateMap<Brand, BrandDto>();
        CreateMap<CreateBrandRequest, Brand>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore());

        CreateMap<Unit, UnitDto>();
        CreateMap<CreateUnitRequest, Unit>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore());

        // Customers / Suppliers
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CurrentBalance, o => o.MapFrom(s => s.OpeningBalance));
        CreateMap<UpdateCustomerRequest, Customer>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.OpeningBalance, o => o.Ignore())
            .ForMember(d => d.CurrentBalance, o => o.Ignore());

        CreateMap<Supplier, SupplierDto>();
        CreateMap<CreateSupplierRequest, Supplier>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CurrentBalance, o => o.MapFrom(s => s.OpeningBalance));

        // Sales
        CreateMap<Sale, SaleDto>();
        CreateMap<SaleItem, SaleItemDto>();
        CreateMap<Payment, PaymentDto>();
        CreateMap<SaleItemRequest, SaleItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.SaleId, o => o.Ignore())
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.CostPrice, o => o.Ignore())
            .ForMember(d => d.TaxAmount, o => o.Ignore())
            .ForMember(d => d.LineTotal, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore());
        CreateMap<PaymentRequest, Payment>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.SaleId, o => o.Ignore())
            .ForMember(d => d.PaidAt, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore());

        // Purchases
        CreateMap<Purchase, PurchaseDto>();
        CreateMap<PurchaseItem, PurchaseItemDto>();
        CreateMap<PurchaseItemRequest, PurchaseItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.PurchaseId, o => o.Ignore())
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.TaxAmount, o => o.Ignore())
            .ForMember(d => d.LineTotal, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore());

        // Expenses
        CreateMap<Expense, ExpenseDto>();
        CreateMap<CreateExpenseRequest, Expense>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.TenantId, o => o.Ignore());

        // Users
        CreateMap<User, UserDto>()
            .ForMember(d => d.Roles, o => o.Ignore());
    }
}
