using FluentValidation;

namespace Billing.Application.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<DTOs.Products.CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).MaximumLength(50);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpeningStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<DTOs.Products.UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCustomerRequestValidator : AbstractValidator<DTOs.CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCustomerRequestValidator : AbstractValidator<DTOs.UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSupplierRequestValidator : AbstractValidator<DTOs.CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<DTOs.Users.CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).NotEmpty();
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<DTOs.Users.UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Role).NotEmpty();
    }
}

public sealed class CreateSaleRequestValidator : AbstractValidator<DTOs.Sales.CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Sale must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0);
        });
        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(i => i.Amount).GreaterThan(0);
        });
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreatePurchaseRequestValidator : AbstractValidator<DTOs.CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Purchase must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.TaxRate).InclusiveBetween(0, 100);
        });
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<DTOs.Auth.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<DTOs.Auth.RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one digit.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.");
    }
}

public sealed class CreateExpenseRequestValidator : AbstractValidator<DTOs.CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExpenseDate).LessThanOrEqualTo(DateTime.UtcNow);
    }
}
