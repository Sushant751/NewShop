using AutoMapper;
using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.DTOs;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Enums;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using FluentValidation;
using MediatR;

namespace Billing.Application.Commands.Purchases;

public sealed record CreateSupplierCommand(CreateSupplierRequest Supplier) : IRequest<Result<SupplierDto>>;
public sealed record GetSuppliersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<Result<PagedResult<SupplierDto>>>;
public sealed record GetSupplierByIdQuery(Guid Id) : IRequest<Result<SupplierDto>>;

public sealed record CreatePurchaseCommand(CreatePurchaseRequest Purchase) : IRequest<Result<PurchaseDto>>;
public sealed record GetPurchaseByIdQuery(Guid Id) : IRequest<Result<PurchaseDto>>;
public sealed record GetPurchasesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<PurchaseDto>>>;

public sealed class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, Result<SupplierDto>>
{
    private readonly ISupplierRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSupplierRequest> _validator;

    public CreateSupplierHandler(ISupplierRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreateSupplierRequest> validator)
    { _repository = repository; _unitOfWork = unitOfWork; _mapper = mapper; _validator = validator; }

    public async Task<Result<SupplierDto>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Supplier, cancellationToken);
        if (!validation.IsValid)
            return Result<SupplierDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var entity = _mapper.Map<Domain.Entities.Supplier>(request.Supplier);
            var id = await _repository.InsertAsync(entity, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            var created = await _repository.GetByIdAsync(id, null, cancellationToken);
            return Result<SupplierDto>.Ok(_mapper.Map<SupplierDto>(created));
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class GetSuppliersHandler : IRequestHandler<GetSuppliersQuery, Result<PagedResult<SupplierDto>>>
{
    private readonly ISupplierRepository _repository;
    private readonly IMapper _mapper;
    public GetSuppliersHandler(ISupplierRepository repository, IMapper mapper)
    { _repository = repository; _mapper = mapper; }

    public async Task<Result<PagedResult<SupplierDto>>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.Search, null, true, null, cancellationToken);
        return Result<PagedResult<SupplierDto>>.Ok(new PagedResult<SupplierDto>
        {
            Items = items.Select(_mapper.Map<SupplierDto>).ToList(),
            Page = request.Page, PageSize = request.PageSize, Total = total
        });
    }
}

public sealed class GetSupplierByIdHandler : IRequestHandler<GetSupplierByIdQuery, Result<SupplierDto>>
{
    private readonly ISupplierRepository _repository;
    private readonly IMapper _mapper;
    public GetSupplierByIdHandler(ISupplierRepository repository, IMapper mapper)
    { _repository = repository; _mapper = mapper; }

    public async Task<Result<SupplierDto>> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Supplier), request.Id);
        return Result<SupplierDto>.Ok(_mapper.Map<SupplierDto>(entity));
    }
}

public sealed class CreatePurchaseHandler : IRequestHandler<CreatePurchaseCommand, Result<PurchaseDto>>
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;
    private readonly IValidator<CreatePurchaseRequest> _validator;

    public CreatePurchaseHandler(IPurchaseRepository purchaseRepository, IProductRepository productRepository,
        IUnitOfWork unitOfWork, ICurrentUserService currentUser, ICacheService cache,
        IValidator<CreatePurchaseRequest> validator)
    {
        _purchaseRepository = purchaseRepository; _productRepository = productRepository;
        _unitOfWork = unitOfWork; _currentUser = currentUser; _cache = cache; _validator = validator;
    }

    public async Task<Result<PurchaseDto>> Handle(CreatePurchaseCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Purchase, cancellationToken);
        if (!validation.IsValid)
            return Result<PurchaseDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        if (!_currentUser.UserId.HasValue) throw new UnauthorizedException();

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var purchaseNumber = await _purchaseRepository.GeneratePurchaseNumberAsync(_unitOfWork.Transaction, cancellationToken);

            var purchase = new Domain.Entities.Purchase
            {
                PurchaseNumber = purchaseNumber,
                ShopId = request.Purchase.ShopId == Guid.Empty ? null : request.Purchase.ShopId,
                SupplierId = request.Purchase.SupplierId,
                PurchaseDate = DateTime.UtcNow,
                Status = PurchaseStatus.Received,
                DiscountAmount = request.Purchase.DiscountAmount,
                PaidAmount = request.Purchase.PaidAmount,
                Notes = request.Purchase.Notes
            };

            decimal subTotal = 0, taxTotal = 0;
            var items = new List<Domain.Entities.PurchaseItem>();
            foreach (var item in request.Purchase.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId, _unitOfWork.Transaction, cancellationToken)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Product), item.ProductId);

                var lineSubTotal = item.Quantity * item.UnitCost;
                var lineTax = lineSubTotal * (item.TaxRate / 100m);
                var lineTotal = lineSubTotal + lineTax;

                items.Add(new Domain.Entities.PurchaseItem
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TaxRate = item.TaxRate,
                    TaxAmount = lineTax,
                    LineTotal = lineTotal
                });

                subTotal += lineSubTotal;
                taxTotal += lineTax;
            }

            purchase.Items = items;
            purchase.SubTotal = subTotal;
            purchase.TaxAmount = taxTotal;
            purchase.GrandTotal = subTotal + taxTotal - purchase.DiscountAmount;
            purchase.BalanceDue = purchase.GrandTotal - purchase.PaidAmount;

            var purchaseId = await _purchaseRepository.CreatePurchaseAsync(purchase, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await _cache.RemoveByPatternAsync("products:*", cancellationToken);
            await _cache.RemoveByPatternAsync("dashboard:*", cancellationToken);

            var created = await _purchaseRepository.GetWithItemsAsync(purchaseId, null, cancellationToken);
            var supplierName = await _purchaseRepository.GetSupplierNameAsync(created!.SupplierId, null, cancellationToken);
            var dto = new PurchaseDto
            {
                Id = created.Id, PurchaseNumber = created.PurchaseNumber, ShopId = created.ShopId,
                SupplierId = created.SupplierId, SupplierName = supplierName, PurchaseDate = created.PurchaseDate,
                Status = created.Status, SubTotal = created.SubTotal, DiscountAmount = created.DiscountAmount,
                TaxAmount = created.TaxAmount, GrandTotal = created.GrandTotal, PaidAmount = created.PaidAmount,
                BalanceDue = created.BalanceDue, Notes = created.Notes,
                Items = created.Items.Select(i => new PurchaseItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitCost, i.TaxRate, i.TaxAmount, i.LineTotal)).ToList()
            };

            return Result<PurchaseDto>.Ok(dto, "Purchase recorded successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class GetPurchaseByIdHandler : IRequestHandler<GetPurchaseByIdQuery, Result<PurchaseDto>>
{
    private readonly IPurchaseRepository _repository;
    public GetPurchaseByIdHandler(IPurchaseRepository repository) => _repository = repository;

    public async Task<Result<PurchaseDto>> Handle(GetPurchaseByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetWithItemsAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Purchase), request.Id);
        var supplierName = await _repository.GetSupplierNameAsync(entity.SupplierId, null, cancellationToken);
        var dto = new PurchaseDto
        {
            Id = entity.Id, PurchaseNumber = entity.PurchaseNumber, ShopId = entity.ShopId,
            SupplierId = entity.SupplierId, SupplierName = supplierName, PurchaseDate = entity.PurchaseDate,
            Status = entity.Status, SubTotal = entity.SubTotal, DiscountAmount = entity.DiscountAmount,
            TaxAmount = entity.TaxAmount, GrandTotal = entity.GrandTotal, PaidAmount = entity.PaidAmount,
            BalanceDue = entity.BalanceDue, Notes = entity.Notes,
            Items = entity.Items.Select(i => new PurchaseItemDto(i.Id, i.ProductId, i.ProductName, i.Quantity, i.UnitCost, i.TaxRate, i.TaxAmount, i.LineTotal)).ToList()
        };
        return Result<PurchaseDto>.Ok(dto);
    }
}

public sealed class GetPurchasesHandler : IRequestHandler<GetPurchasesQuery, Result<PagedResult<PurchaseDto>>>
{
    private readonly IPurchaseRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMapper _mapper;
    public GetPurchasesHandler(IPurchaseRepository repository, ISupplierRepository supplierRepository, IMapper mapper)
    { _repository = repository; _supplierRepository = supplierRepository; _mapper = mapper; }

    public async Task<Result<PagedResult<PurchaseDto>>> Handle(GetPurchasesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, null, null, true, null, cancellationToken);
        var suppliers = (await _supplierRepository.GetAllAsync(null, cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        
        var dtos = items.Select(p =>
        {
            var dto = _mapper.Map<PurchaseDto>(p);
            return dto with { SupplierName = suppliers.GetValueOrDefault(p.SupplierId) };
        }).ToList();

        return Result<PagedResult<PurchaseDto>>.Ok(new PagedResult<PurchaseDto>
        {
            Items = dtos,
            Page = request.Page, PageSize = request.PageSize, Total = total
        });
    }
}
