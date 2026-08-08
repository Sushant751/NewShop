using AutoMapper;
using Billing.Application.Abstractions;
using Billing.Application.Commands.Products;
using Billing.Application.Common;
using Billing.Application.DTOs.Products;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Constants;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using FluentValidation;
using MediatR;

namespace Billing.Application.Handlers.Products;

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProductRequest> _validator;
    private readonly ICacheService _cache;

    public CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreateProductRequest> validator, ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
        _cache = cache;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Product, cancellationToken);
        if (!validation.IsValid)
            return Result<ProductDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Product.Sku))
            {
                var existing = await _repository.GetBySkuAsync(request.Product.Sku, _unitOfWork.Transaction, cancellationToken);
                if (existing is not null)
                    throw new ConflictException($"A product with SKU '{request.Product.Sku}' already exists.");
            }

            var entity = _mapper.Map<Product>(request.Product);
            var id = await _repository.InsertAsync(entity, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await _cache.RemoveByPatternAsync($"products:*", cancellationToken);

            var created = await _repository.GetByIdAsync(id, null, cancellationToken);
            return Result<ProductDto>.Ok(_mapper.Map<ProductDto>(created));
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateProductRequest> _validator;
    private readonly ICacheService _cache;

    public UpdateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<UpdateProductRequest> validator, ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
        _cache = cache;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Product, cancellationToken);
        if (!validation.IsValid)
            return Result.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        var existing = await _repository.GetByIdAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _mapper.Map(request.Product, existing);
            existing.Id = request.Id;
            await _repository.UpdateAsync(existing, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveAsync(CacheKeys.Product(existing.TenantId, request.Id), cancellationToken);
            await _cache.RemoveByPatternAsync("products:*", cancellationToken);
            return Result.Ok("Product updated successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUser;

    public DeleteProductHandler(IProductRepository repository, IUnitOfWork unitOfWork, ICacheService cache,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedException();
        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            await _repository.SoftDeleteAsync(request.Id, _currentUser.UserId.Value, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveByPatternAsync("products:*", cancellationToken);
            return Result.Ok("Product deleted successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
