using Billing.Application.DTOs;
using Billing.Application.DTOs.Auth;
using Billing.Application.DTOs.Products;
using Billing.Application.DTOs.Sales;
using Billing.Application.Validators;
using Billing.Shared.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Billing.UnitTests;

/// <summary>
/// Unit tests for all FluentValidation validators. These are pure tests that
/// exercise validation rules without any database or HTTP dependencies.
/// </summary>
public class ValidatorTests
{
    // ── CreateProductRequestValidator ──────────────────────────────────────

    [Fact]
    public void CreateProduct_Valid_Request_Should_Pass_Validation()
    {
        // Arrange
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            Name: "Test Product",
            Description: "A test product",
            Sku: "TST-001",
            Barcode: "1234567890",
            CategoryId: Guid.NewGuid(),
            BrandId: Guid.NewGuid(),
            UnitId: Guid.NewGuid(),
            CostPrice: 10m,
            SellingPrice: 20m,
            TaxRate: 5m,
            IsTaxable: true,
            ReorderLevel: 5,
            OpeningStock: 100,
            ImageUrl: null,
            TrackInventory: true,
            AllowSaleWithoutStock: false);

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProduct_Empty_Name_Should_Fail()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            Name: "", Description: null, Sku: null, Barcode: null,
            CategoryId: null, BrandId: null, UnitId: null,
            CostPrice: 0, SellingPrice: 0, TaxRate: 0, IsTaxable: false,
            ReorderLevel: 0, OpeningStock: 0, ImageUrl: null,
            TrackInventory: true, AllowSaleWithoutStock: false);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateProduct_Negative_Prices_Should_Fail()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            Name: "Test", Description: null, Sku: null, Barcode: null,
            CategoryId: null, BrandId: null, UnitId: null,
            CostPrice: -1, SellingPrice: -5, TaxRate: 0, IsTaxable: false,
            ReorderLevel: 0, OpeningStock: 0, ImageUrl: null,
            TrackInventory: true, AllowSaleWithoutStock: false);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.CostPrice);
        result.ShouldHaveValidationErrorFor(x => x.SellingPrice);
    }

    [Fact]
    public void CreateProduct_TaxRate_Above_100_Should_Fail()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            Name: "Test", Description: null, Sku: null, Barcode: null,
            CategoryId: null, BrandId: null, UnitId: null,
            CostPrice: 10, SellingPrice: 20, TaxRate: 150, IsTaxable: true,
            ReorderLevel: 0, OpeningStock: 0, ImageUrl: null,
            TrackInventory: true, AllowSaleWithoutStock: false);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.TaxRate);
    }

    [Fact]
    public void CreateProduct_Name_Too_Long_Should_Fail()
    {
        var validator = new CreateProductRequestValidator();
        var request = new CreateProductRequest(
            Name: new string('A', 201), Description: null, Sku: null, Barcode: null,
            CategoryId: null, BrandId: null, UnitId: null,
            CostPrice: 0, SellingPrice: 0, TaxRate: 0, IsTaxable: false,
            ReorderLevel: 0, OpeningStock: 0, ImageUrl: null,
            TrackInventory: true, AllowSaleWithoutStock: false);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ── CreateSaleRequestValidator ─────────────────────────────────────────

    [Fact]
    public void CreateSale_Valid_Request_Should_Pass_Validation()
    {
        var validator = new CreateSaleRequestValidator();
        var request = new CreateSaleRequest(
            CustomerId: Guid.NewGuid(),
            ShopId: Guid.NewGuid(),
            Items: new List<SaleItemRequest>
            {
                new(Guid.NewGuid(), 2, 10m, 0m)
            },
            Payments: new List<PaymentRequest>
            {
                new(PaymentMethod.Cash, 20m, null, null)
            },
            DiscountAmount: 0m,
            Notes: null,
            CouponCode: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSale_Empty_Items_Should_Fail()
    {
        var validator = new CreateSaleRequestValidator();
        var request = new CreateSaleRequest(
            CustomerId: null, ShopId: null,
            Items: new List<SaleItemRequest>(),
            Payments: new List<PaymentRequest>(),
            DiscountAmount: 0, Notes: null, CouponCode: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Items");
    }

    [Fact]
    public void CreateSale_Item_With_Zero_Quantity_Should_Fail()
    {
        var validator = new CreateSaleRequestValidator();
        var request = new CreateSaleRequest(
            CustomerId: null, ShopId: null,
            Items: new List<SaleItemRequest>
            {
                new(Guid.NewGuid(), 0, 10m, 0m)
            },
            Payments: new List<PaymentRequest>(),
            DiscountAmount: 0, Notes: null, CouponCode: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void CreateSale_Item_With_Negative_UnitPrice_Should_Fail()
    {
        var validator = new CreateSaleRequestValidator();
        var request = new CreateSaleRequest(
            CustomerId: null, ShopId: null,
            Items: new List<SaleItemRequest>
            {
                new(Guid.NewGuid(), 1, -5m, 0m)
            },
            Payments: new List<PaymentRequest>(),
            DiscountAmount: 0, Notes: null, CouponCode: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void CreateSale_Payment_With_Zero_Amount_Should_Fail()
    {
        var validator = new CreateSaleRequestValidator();
        var request = new CreateSaleRequest(
            CustomerId: null, ShopId: null,
            Items: new List<SaleItemRequest> { new(Guid.NewGuid(), 1, 10m, 0m) },
            Payments: new List<PaymentRequest> { new(PaymentMethod.Cash, 0, null, null) },
            DiscountAmount: 0, Notes: null, CouponCode: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Payments[0].Amount");
    }

    // ── LoginRequestValidator ──────────────────────────────────────────────

    [Fact]
    public void Login_Valid_Request_Should_Pass_Validation()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("user@example.com", "Password123", null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Login_Empty_Email_Should_Fail()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("", "Password123", null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Login_Invalid_Email_Format_Should_Fail()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("not-an-email", "Password123", null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Login_Short_Password_Should_Fail()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("user@example.com", "12345", null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ── RegisterRequestValidator ───────────────────────────────────────────

    [Fact]
    public void Register_Valid_Request_Should_Pass_Validation()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest(
            FullName: "John Doe",
            Email: "john@example.com",
            Password: "Password1",
            PhoneNumber: "555-1234",
            TenantName: "Acme Corp");

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Register_Empty_FullName_Should_Fail()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("", "john@example.com", "Password1", null, "Acme");

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Register_Password_Without_Digit_Should_Fail()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("John Doe", "john@example.com", "NoDigitsHere", null, "Acme");

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Register_Password_Without_Uppercase_Should_Fail()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("John Doe", "john@example.com", "alllowercase1", null, "Acme");

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Register_Short_Password_Should_Fail()
    {
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("John Doe", "john@example.com", "Short1", null, "Acme");

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ── CreateCustomerRequestValidator ─────────────────────────────────────

    [Fact]
    public void CreateCustomer_Valid_Request_Should_Pass_Validation()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest(
            Name: "Jane Smith", Email: "jane@example.com", Phone: "555-9999",
            Address: "123 Main St", City: "NYC", State: "NY", Country: "USA",
            PostalCode: "10001", TaxNumber: "TAX123", OpeningBalance: 0, CreditLimit: 1000);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomer_Empty_Name_Should_Fail()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest(
            Name: "", Email: null, Phone: null, Address: null,
            City: null, State: null, Country: null, PostalCode: null,
            TaxNumber: null, OpeningBalance: 0, CreditLimit: 0);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateCustomer_Invalid_Email_Should_Fail()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest(
            Name: "Jane", Email: "not-email", Phone: null, Address: null,
            City: null, State: null, Country: null, PostalCode: null,
            TaxNumber: null, OpeningBalance: 0, CreditLimit: 0);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void CreateCustomer_Negative_CreditLimit_Should_Fail()
    {
        var validator = new CreateCustomerRequestValidator();
        var request = new CreateCustomerRequest(
            Name: "Jane", Email: null, Phone: null, Address: null,
            City: null, State: null, Country: null, PostalCode: null,
            TaxNumber: null, OpeningBalance: 0, CreditLimit: -100);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.CreditLimit);
    }

    // ── CreateSupplierRequestValidator ─────────────────────────────────────

    [Fact]
    public void CreateSupplier_Valid_Request_Should_Pass_Validation()
    {
        var validator = new CreateSupplierRequestValidator();
        var request = new CreateSupplierRequest(
            Name: "Acme Supplies", ContactPerson: "Bob", Email: "bob@acme.com",
            Phone: "555-0000", Address: "456 Oak Ave", City: "LA", State: "CA",
            Country: "USA", PostalCode: "90001", TaxNumber: "TAX456", OpeningBalance: 0);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateSupplier_Empty_Name_Should_Fail()
    {
        var validator = new CreateSupplierRequestValidator();
        var request = new CreateSupplierRequest(
            Name: "", ContactPerson: null, Email: null, Phone: null,
            Address: null, City: null, State: null, Country: null,
            PostalCode: null, TaxNumber: null, OpeningBalance: 0);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateSupplier_Negative_OpeningBalance_Should_Fail()
    {
        var validator = new CreateSupplierRequestValidator();
        var request = new CreateSupplierRequest(
            Name: "Acme", ContactPerson: null, Email: null, Phone: null,
            Address: null, City: null, State: null, Country: null,
            PostalCode: null, TaxNumber: null, OpeningBalance: -50);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.OpeningBalance);
    }

    // ── CreatePurchaseRequestValidator ─────────────────────────────────────

    [Fact]
    public void CreatePurchase_Valid_Request_Should_Pass_Validation()
    {
        var validator = new CreatePurchaseRequestValidator();
        var request = new CreatePurchaseRequest(
            SupplierId: Guid.NewGuid(),
            ShopId: Guid.NewGuid(),
            Items: new List<PurchaseItemRequest>
            {
                new(Guid.NewGuid(), 10, 5m, 5m)
            },
            DiscountAmount: 0,
            PaidAmount: 50,
            Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePurchase_Empty_SupplierId_Should_Fail()
    {
        var validator = new CreatePurchaseRequestValidator();
        var request = new CreatePurchaseRequest(
            SupplierId: Guid.Empty, ShopId: null,
            Items: new List<PurchaseItemRequest> { new(Guid.NewGuid(), 1, 1m, 0m) },
            DiscountAmount: 0, PaidAmount: 0, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.SupplierId);
    }

    [Fact]
    public void CreatePurchase_Empty_Items_Should_Fail()
    {
        var validator = new CreatePurchaseRequestValidator();
        var request = new CreatePurchaseRequest(
            SupplierId: Guid.NewGuid(), ShopId: null,
            Items: new List<PurchaseItemRequest>(),
            DiscountAmount: 0, PaidAmount: 0, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Items");
    }

    [Fact]
    public void CreatePurchase_Item_With_Zero_Quantity_Should_Fail()
    {
        var validator = new CreatePurchaseRequestValidator();
        var request = new CreatePurchaseRequest(
            SupplierId: Guid.NewGuid(), ShopId: null,
            Items: new List<PurchaseItemRequest> { new(Guid.NewGuid(), 0, 5m, 0m) },
            DiscountAmount: 0, PaidAmount: 0, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    // ── CreateExpenseRequestValidator ──────────────────────────────────────

    [Fact]
    public void CreateExpense_Valid_Request_Should_Pass_Validation()
    {
        var validator = new CreateExpenseRequestValidator();
        var request = new CreateExpenseRequest(
            Title: "Office Supplies", CategoryId: Guid.NewGuid(),
            Amount: 50m, ExpenseDate: DateTime.UtcNow.AddDays(-1),
            PaymentMethod: PaymentMethod.Cash, Reference: "REF001", Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateExpense_Empty_Title_Should_Fail()
    {
        var validator = new CreateExpenseRequestValidator();
        var request = new CreateExpenseRequest(
            Title: "", CategoryId: null, Amount: 10, ExpenseDate: DateTime.UtcNow,
            PaymentMethod: PaymentMethod.Cash, Reference: null, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateExpense_Zero_Amount_Should_Fail()
    {
        var validator = new CreateExpenseRequestValidator();
        var request = new CreateExpenseRequest(
            Title: "Test", CategoryId: null, Amount: 0, ExpenseDate: DateTime.UtcNow,
            PaymentMethod: PaymentMethod.Cash, Reference: null, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void CreateExpense_Future_Date_Should_Fail()
    {
        var validator = new CreateExpenseRequestValidator();
        var request = new CreateExpenseRequest(
            Title: "Test", CategoryId: null, Amount: 10,
            ExpenseDate: DateTime.UtcNow.AddDays(1),
            PaymentMethod: PaymentMethod.Cash, Reference: null, Notes: null);

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.ExpenseDate);
    }
}
