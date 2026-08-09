using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.DTOs.Sales;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Enums;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Commands.Sales;

public sealed record CreateSaleCommand(CreateSaleRequest Sale) : IRequest<Result<SaleDto>>;
public sealed record CancelSaleCommand(Guid SaleId, string? Reason) : IRequest<Result>;
public sealed record GetSaleByIdQuery(Guid Id) : IRequest<Result<SaleDto>>;
public sealed record GetSalesQuery(DateTime? From, DateTime? To, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<SaleDto>>>;
public sealed record GetDashboardQuery(DateTime? From, DateTime? To, bool IsGlobalAdmin = false) : IRequest<Result<DTOs.DashboardDto>>;

public sealed class CreateSaleHandler : IRequestHandler<CreateSaleCommand, Result<SaleDto>>
{
    private readonly ISalesRepository _salesRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;
    private readonly FluentValidation.IValidator<CreateSaleRequest> _validator;

    public CreateSaleHandler(ISalesRepository salesRepository, IProductRepository productRepository,
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache,
        FluentValidation.IValidator<CreateSaleRequest> validator)
    {
        _salesRepository = salesRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
        _validator = validator;
    }

    public async Task<Result<SaleDto>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Sale, cancellationToken);
        if (!validation.IsValid)
            return Result<SaleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        if (!_currentUser.UserId.HasValue || !_currentUser.TenantId.HasValue)
            throw new UnauthorizedException();

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var invoiceNumber = await _salesRepository.GenerateInvoiceNumberAsync(_unitOfWork.Transaction, cancellationToken);

            var sale = new Domain.Entities.Sale
            {
                InvoiceNumber = invoiceNumber,
                ShopId = request.Sale.ShopId,
                CustomerId = request.Sale.CustomerId,
                CashierId = _currentUser.UserId.Value,
                SaleDate = DateTime.UtcNow,
                Status = SaleStatus.Completed,
                Notes = request.Sale.Notes,
                CouponCode = request.Sale.CouponCode,
                DiscountAmount = request.Sale.DiscountAmount
            };

            decimal subTotal = 0, taxTotal = 0;
            var items = new List<Domain.Entities.SaleItem>();
            foreach (var item in request.Sale.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, _unitOfWork.Transaction, cancellationToken)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Product), item.ProductId);

                if (product.TrackInventory && !product.AllowSaleWithoutStock && product.CurrentStock < item.Quantity)
                    throw new ConflictException($"Insufficient stock for product '{product.Name}'. Available: {product.CurrentStock}, Requested: {item.Quantity}");

                var lineSubTotal = item.Quantity * item.UnitPrice - item.DiscountAmount;
                var lineTax = product.IsTaxable ? lineSubTotal * (product.TaxRate / 100m) : 0;
                var lineTotal = lineSubTotal + lineTax;

                items.Add(new Domain.Entities.SaleItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    CostPrice = product.CostPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxRate = product.TaxRate,
                    TaxAmount = lineTax,
                    LineTotal = lineTotal
                });

                subTotal += lineSubTotal;
                taxTotal += lineTax;
            }

            sale.Items = items;
            sale.SubTotal = subTotal;
            sale.TaxAmount = taxTotal;
            sale.GrandTotal = subTotal + taxTotal - sale.DiscountAmount;
            sale.RoundOff = Math.Round(sale.GrandTotal) - sale.GrandTotal;
            sale.GrandTotal = Math.Round(sale.GrandTotal);

            var paidAmount = request.Sale.Payments.Sum(p => p.Amount);
            sale.PaidAmount = paidAmount;
            sale.PaymentStatus = paidAmount >= sale.GrandTotal ? PaymentStatus.Paid
                : paidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid;
            sale.BalanceDue = sale.GrandTotal - paidAmount;

            sale.Payments = request.Sale.Payments.Select(p => new Domain.Entities.Payment
            {
                Method = p.Method,
                Amount = p.Amount,
                Reference = p.Reference,
                Notes = p.Notes,
                PaidAt = DateTime.UtcNow
            }).ToList();

            var saleId = await _salesRepository.CreateSaleAsync(sale, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await _cache.RemoveByPatternAsync("dashboard:*", cancellationToken);
            await _cache.RemoveByPatternAsync("products:*", cancellationToken);

            var created = await _salesRepository.GetWithItemsAsync(saleId, null, cancellationToken);
            var dto = new SaleDto(
                created!.Id, created.InvoiceNumber, created.ShopId, created.CustomerId, null,
                created.CashierId, created.SaleDate, created.Status, created.PaymentStatus,
                created.SubTotal, created.DiscountAmount, created.TaxAmount, created.RoundOff,
                created.GrandTotal, created.PaidAmount, created.BalanceDue, created.Notes,
                created.Items.Select(i => new SaleItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.DiscountAmount, i.TaxRate, i.TaxAmount, i.LineTotal)).ToList(),
                created.Payments.Select(p => new PaymentDto(p.Id, p.Method, p.Amount, p.Reference, p.PaidAt)).ToList());

            return Result<SaleDto>.Ok(dto, "Sale completed successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class GetSaleByIdHandler : IRequestHandler<GetSaleByIdQuery, Result<SaleDto>>
{
    private readonly ISalesRepository _salesRepository;

    public GetSaleByIdHandler(ISalesRepository salesRepository)
    {
        _salesRepository = salesRepository;
    }

    public async Task<Result<SaleDto>> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
    {
        var sale = await _salesRepository.GetWithItemsAsync(request.Id, null, cancellationToken);
        if (sale is null)
            return Result<SaleDto>.Fail($"Sale with id '{request.Id}' was not found.");

        var dto = SaleMapper.ToDto(sale);
        return Result<SaleDto>.Ok(dto);
    }
}

public sealed class GetSalesHandler : IRequestHandler<GetSalesQuery, Result<PagedResult<SaleDto>>>
{
    private readonly ISalesRepository _salesRepository;

    public GetSalesHandler(ISalesRepository salesRepository)
    {
        _salesRepository = salesRepository;
    }

    public async Task<Result<PagedResult<SaleDto>>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        IReadOnlyList<Domain.Entities.Sale> sales;
        if (request.From.HasValue && request.To.HasValue)
        {
            sales = await _salesRepository.GetByDateRangeAsync(request.From.Value, request.To.Value, null, cancellationToken);
        }
        else
        {
            sales = await _salesRepository.GetAllAsync(null, cancellationToken);
        }

        var allDtos = sales.Select(SaleMapper.ToDto).ToList();
        var total = allDtos.Count;
        var paged = allDtos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<SaleDto>
        {
            Items = paged,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
        return Result<PagedResult<SaleDto>>.Ok(result);
    }
}

public sealed class CancelSaleHandler : IRequestHandler<CancelSaleCommand, Result>
{
    private readonly ISalesRepository _salesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public CancelSaleHandler(ISalesRepository salesRepository, IUnitOfWork unitOfWork,
        ICurrentUserService currentUser, ICacheService cache)
    {
        _salesRepository = salesRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) throw new UnauthorizedException();
        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var affected = await _salesRepository.CancelSaleAsync(request.SaleId, _currentUser.UserId.Value, _unitOfWork.Transaction, cancellationToken);
            if (affected == 0)
                throw new NotFoundException(nameof(Domain.Entities.Sale), request.SaleId);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveByPatternAsync("dashboard:*", cancellationToken);
            await _cache.RemoveByPatternAsync("products:*", cancellationToken);
            return Result.Ok("Sale cancelled and stock restored.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>
/// Centralized mapping from <see cref="Domain.Entities.Sale"/> to <see cref="SaleDto"/>.
/// Kept as a static helper so both the create and query handlers share one mapping path.
/// </summary>
file static class SaleMapper
{
    public static SaleDto ToDto(Domain.Entities.Sale sale) => new(
        sale.Id,
        sale.InvoiceNumber,
        sale.ShopId,
        sale.CustomerId,
        sale.CustomerName,
        sale.CashierId,
        sale.SaleDate,
        sale.Status,
        sale.PaymentStatus,
        sale.SubTotal,
        sale.DiscountAmount,
        sale.TaxAmount,
        sale.RoundOff,
        sale.GrandTotal,
        sale.PaidAmount,
        sale.BalanceDue,
        sale.Notes,
        sale.Items.Select(i => new SaleItemDto(
            i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice,
            i.DiscountAmount, i.TaxRate, i.TaxAmount, i.LineTotal)).ToList(),
        sale.Payments.Select(p => new PaymentDto(
            p.Id, p.Method, p.Amount, p.Reference, p.PaidAt)).ToList());
}
