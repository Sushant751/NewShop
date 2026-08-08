using AutoMapper;
using Billing.Application.Abstractions;
using Billing.Application.Commands.Products;
using Billing.Application.Common;
using Billing.Application.DTOs.Products;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.TenantContext;
using Billing.Shared.Constants;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Handlers.Products;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetProductByIdHandler(IProductRepository repository, IMapper mapper, ICacheService cache, ITenantContext tenantContext)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Product(_tenantContext.TenantId!.Value, request.Id);
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null) return Result<ProductDto>.Ok(cached);

        var entity = await _repository.GetByIdAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        var dto = _mapper.Map<ProductDto>(entity);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);
        return Result<ProductDto>.Ok(dto);
    }
}

public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetProductsHandler(IProductRepository repository, IMapper mapper, ICacheService cache, ITenantContext tenantContext)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProductList(_tenantContext.TenantId!.Value, request.Page, request.PageSize, request.Search);
        var cached = await _cache.GetAsync<PagedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cached is not null) return Result<PagedResult<ProductDto>>.Ok(cached);

        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.Search, request.OrderBy, request.Ascending, null, cancellationToken);
        var dtos = items.Select(_mapper.Map<ProductDto>).ToList();
        var result = new PagedResult<ProductDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        };
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return Result<PagedResult<ProductDto>>.Ok(result);
    }
}

public sealed class SearchProductsHandler : IRequestHandler<SearchProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public SearchProductsHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.SearchAsync(request.Term, request.Limit, null, cancellationToken);
        return Result<IReadOnlyList<ProductDto>>.Ok(items.Select(_mapper.Map<ProductDto>).ToList());
    }
}

public sealed class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public GetLowStockProductsHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetLowStockAsync(null, null, cancellationToken);
        return Result<IReadOnlyList<ProductDto>>.Ok(items.Select(_mapper.Map<ProductDto>).ToList());
    }
}
