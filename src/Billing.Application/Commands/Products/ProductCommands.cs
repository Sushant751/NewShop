using Billing.Application.Common;
using Billing.Application.DTOs.Products;
using Billing.Shared.Results;
using MediatR;

namespace Billing.Application.Commands.Products;

public sealed record CreateProductCommand(CreateProductRequest Product) : IRequest<Result<ProductDto>>;
public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Product) : IRequest<Result>;
public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;
public sealed record GetProductsQuery(int Page = 1, int PageSize = 20, string? Search = null, string? OrderBy = null, bool Ascending = true)
    : IRequest<Result<PagedResult<ProductDto>>>;
public sealed record SearchProductsQuery(string Term, int Limit = 20) : IRequest<Result<IReadOnlyList<ProductDto>>>;
public sealed record GetLowStockProductsQuery() : IRequest<Result<IReadOnlyList<ProductDto>>>;
