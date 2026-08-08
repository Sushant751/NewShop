using System.Data;
using Billing.Application.Abstractions;
using Billing.Application.Commands.Sales;
using Billing.Application.DTOs.Sales;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Enums;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using FluentAssertions;
using FluentValidation;
using Moq;
using Xunit;

namespace Billing.UnitTests;

/// <summary>
/// Unit tests for <see cref="CreateSaleHandler"/>. Uses Moq to isolate the
/// handler from its repository, unit-of-work, cache, and current-user
/// dependencies so we can verify business logic in isolation.
/// </summary>
public class CreateSaleHandlerTests
{
    private readonly Mock<ISalesRepository> _salesRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IValidator<CreateSaleRequest>> _validator = new();

    private readonly CreateSaleHandler _handler;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public CreateSaleHandlerTests()
    {
        _handler = new CreateSaleHandler(
            _salesRepo.Object, _productRepo.Object, _unitOfWork.Object,
            _currentUser.Object, _cache.Object, _validator.Object);
    }

    private static Product CreateProduct(
        decimal currentStock = 100,
        bool trackInventory = true,
        bool allowSaleWithoutStock = false,
        bool isTaxable = true,
        decimal taxRate = 5m,
        decimal costPrice = 8m) => new()
        {
            Id = ProductId,
            Name = "Test Product",
            CurrentStock = currentStock,
            TrackInventory = trackInventory,
            AllowSaleWithoutStock = allowSaleWithoutStock,
            IsTaxable = isTaxable,
            TaxRate = taxRate,
            CostPrice = costPrice,
            IsActive = true
        };

    private static CreateSaleRequest CreateValidSaleRequest() => new(
        CustomerId: Guid.NewGuid(),
        ShopId: Guid.NewGuid(),
        Items: new List<SaleItemRequest>
        {
            new(ProductId, 2, 10m, 0m)
        },
        Payments: new List<PaymentRequest>
        {
            new(PaymentMethod.Cash, 21m, null, null)
        },
        DiscountAmount: 0m,
        Notes: null,
        CouponCode: null);

    private void SetupValidUser()
    {
        _currentUser.SetupGet(x => x.UserId).Returns(UserId);
        _currentUser.SetupGet(x => x.TenantId).Returns(TenantId);
    }

    private void SetupValidValidator()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateSaleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    [Fact]
    public async Task Handle_Validation_Failure_Should_Return_Failed_Result()
    {
        // Arrange
        SetupValidUser();
        var validationFailures = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("Items", "Sale must contain at least one item.") });
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateSaleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationFailures);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Unauthenticated_User_Should_Throw_UnauthorizedException()
    {
        // Arrange
        SetupValidValidator();
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_Missing_TenantId_Should_Throw_UnauthorizedException()
    {
        // Arrange
        SetupValidValidator();
        _currentUser.SetupGet(x => x.UserId).Returns(UserId);
        _currentUser.SetupGet(x => x.TenantId).Returns((Guid?)null);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_Product_Not_Found_Should_Throw_NotFoundException()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Insufficient_Stock_Should_Throw_ConflictException()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct(currentStock: 1)); // requesting 2, only 1 available

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllowSaleWithoutStock_Should_Not_Check_Stock()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();
        var product = CreateProduct(currentStock: 0, allowSaleWithoutStock: true);
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var saleId = Guid.NewGuid();
        _salesRepo
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-0001");
        _salesRepo
            .Setup(r => r.CreateSaleAsync(It.IsAny<Sale>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saleId);

        var createdSale = new Sale
        {
            Id = saleId,
            InvoiceNumber = "INV-0001",
            CashierId = UserId,
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            SubTotal = 20m,
            TaxAmount = 1m,
            GrandTotal = 21m,
            PaidAmount = 21m,
            BalanceDue = 0m,
            Items = new List<SaleItem>(),
            Payments = new List<Payment>()
        };
        _salesRepo
            .Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSale);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Successful_Sale_Should_Return_Ok_Result()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();

        var product = CreateProduct(currentStock: 100, taxRate: 5m, costPrice: 8m);
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var saleId = Guid.NewGuid();
        _salesRepo
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-0001");
        _salesRepo
            .Setup(r => r.CreateSaleAsync(It.IsAny<Sale>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saleId);

        var createdSale = new Sale
        {
            Id = saleId,
            InvoiceNumber = "INV-0001",
            CashierId = UserId,
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            SubTotal = 20m,
            TaxAmount = 1m,
            GrandTotal = 21m,
            PaidAmount = 21m,
            BalanceDue = 0m,
            Items = new List<SaleItem>
            {
                new() { ProductId = ProductId, ProductName = "Test Product", Quantity = 2, UnitPrice = 10m, LineTotal = 21m }
            },
            Payments = new List<Payment>
            {
                new() { Method = PaymentMethod.Cash, Amount = 21m }
            }
        };
        _salesRepo
            .Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSale);

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.InvoiceNumber.Should().Be("INV-0001");
        result.Data.Items.Should().HaveCount(1);
        result.Data.Payments.Should().HaveCount(1);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPatternAsync("dashboard:*", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPatternAsync("products:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Partial_Payment_Should_Set_PaymentStatus_Partial()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();

        var product = CreateProduct(currentStock: 100, taxRate: 0m);
        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var saleId = Guid.NewGuid();
        _salesRepo.Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-0002");
        _salesRepo.Setup(r => r.CreateSaleAsync(It.IsAny<Sale>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saleId);

        // Sale total = 2 * 10 = 20, paid = 10 → partial
        var createdSale = new Sale
        {
            Id = saleId,
            InvoiceNumber = "INV-0002",
            CashierId = UserId,
            SaleDate = DateTime.UtcNow,
            Status = SaleStatus.Completed,
            PaymentStatus = PaymentStatus.Partial,
            SubTotal = 20m,
            TaxAmount = 0m,
            GrandTotal = 20m,
            PaidAmount = 10m,
            BalanceDue = 10m,
            Items = new List<SaleItem>(),
            Payments = new List<Payment>()
        };
        _salesRepo.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSale);

        var request = new CreateSaleCommand(new CreateSaleRequest(
            CustomerId: null, ShopId: null,
            Items: new List<SaleItemRequest> { new(ProductId, 2, 10m, 0m) },
            Payments: new List<PaymentRequest> { new(PaymentMethod.Cash, 10m, null, null) },
            DiscountAmount: 0, Notes: null, CouponCode: null));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.PaymentStatus.Should().Be(PaymentStatus.Partial);
        result.Data.BalanceDue.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_Repository_Exception_Should_Rollback_And_Rethrow()
    {
        // Arrange
        SetupValidUser();
        SetupValidValidator();

        _productRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct());

        _salesRepo
            .Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-0003");
        _salesRepo
            .Setup(r => r.CreateSaleAsync(It.IsAny<Sale>(), It.IsAny<IDbTransaction?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var request = new CreateSaleCommand(CreateValidSaleRequest());

        // Act
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
