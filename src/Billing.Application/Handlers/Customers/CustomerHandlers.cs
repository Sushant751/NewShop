using AutoMapper;
using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.DTOs;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Shared.Exceptions;
using Billing.Shared.Results;
using FluentValidation;
using MediatR;

namespace Billing.Application.Commands.Customers;

public sealed record CreateCustomerCommand(CreateCustomerRequest Customer) : IRequest<Result<CustomerDto>>;
public sealed record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Customer) : IRequest<Result>;
public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result>;
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;
public sealed record GetCustomersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<Result<PagedResult<CustomerDto>>>;
public sealed record SearchCustomersQuery(string Term, int Limit = 20) : IRequest<Result<IReadOnlyList<CustomerDto>>>;

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCustomerRequest> _validator;
    private readonly ICacheService _cache;

    public CreateCustomerHandler(ICustomerRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreateCustomerRequest> validator, ICacheService cache)
    {
        _repository = repository; _unitOfWork = unitOfWork; _mapper = mapper; _validator = validator; _cache = cache;
    }

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Customer, cancellationToken);
        if (!validation.IsValid)
            return Result<CustomerDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var entity = _mapper.Map<Domain.Entities.Customer>(request.Customer);
            var id = await _repository.InsertAsync(entity, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveByPatternAsync("customers:*", cancellationToken);
            var created = await _repository.GetByIdAsync(id, null, cancellationToken);
            return Result<CustomerDto>.Ok(_mapper.Map<CustomerDto>(created));
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateCustomerRequest> _validator;
    private readonly ICacheService _cache;

    public UpdateCustomerHandler(ICustomerRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<UpdateCustomerRequest> validator, ICacheService cache)
    {
        _repository = repository; _unitOfWork = unitOfWork; _mapper = mapper; _validator = validator; _cache = cache;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request.Customer, cancellationToken);
        if (!validation.IsValid)
            return Result.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        var existing = await _repository.GetByIdAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _mapper.Map(request.Customer, existing);
            existing.Id = request.Id;
            await _repository.UpdateAsync(existing, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveByPatternAsync("customers:*", cancellationToken);
            return Result.Ok("Customer updated successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUser;

    public DeleteCustomerHandler(ICustomerRepository repository, IUnitOfWork unitOfWork, ICacheService cache,
        ICurrentUserService currentUser)
    {
        _repository = repository; _unitOfWork = unitOfWork; _cache = cache; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue) throw new UnauthorizedException();
        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            await _repository.SoftDeleteAsync(request.Id, _currentUser.UserId.Value, _unitOfWork.Transaction, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            await _cache.RemoveByPatternAsync("customers:*", cancellationToken);
            return Result.Ok("Customer deleted successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _repository;
    private readonly IMapper _mapper;
    public GetCustomerByIdHandler(ICustomerRepository repository, IMapper mapper)
    { _repository = repository; _mapper = mapper; }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, null, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);
        return Result<CustomerDto>.Ok(_mapper.Map<CustomerDto>(entity));
    }
}

public sealed class GetCustomersHandler : IRequestHandler<GetCustomersQuery, Result<PagedResult<CustomerDto>>>
{
    private readonly ICustomerRepository _repository;
    private readonly IMapper _mapper;
    public GetCustomersHandler(ICustomerRepository repository, IMapper mapper)
    { _repository = repository; _mapper = mapper; }

    public async Task<Result<PagedResult<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(request.Page, request.PageSize, request.Search, null, true, null, cancellationToken);
        return Result<PagedResult<CustomerDto>>.Ok(new PagedResult<CustomerDto>
        {
            Items = items.Select(_mapper.Map<CustomerDto>).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total
        });
    }
}

public sealed class SearchCustomersHandler : IRequestHandler<SearchCustomersQuery, Result<IReadOnlyList<CustomerDto>>>
{
    private readonly ICustomerRepository _repository;
    private readonly IMapper _mapper;
    public SearchCustomersHandler(ICustomerRepository repository, IMapper mapper)
    { _repository = repository; _mapper = mapper; }

    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.SearchAsync(request.Term, request.Limit, null, cancellationToken);
        return Result<IReadOnlyList<CustomerDto>>.Ok(items.Select(_mapper.Map<CustomerDto>).ToList());
    }
}
