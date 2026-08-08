using Billing.Application.Commands.Users;
using Billing.Application.DTOs.Users;
using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Persistence.Repositories;
using Billing.Persistence.UnitOfWork;
using Billing.Persistence.TenantContext;
using Billing.Shared.Results;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Billing.Application.Handlers.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _repository;

    public GetUsersHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // For brevity in this implementation, we will just fetch users and return basic DTOs.
        // A complete implementation would map roles too.
        var users = await _repository.GetAllAsync(null, cancellationToken);
        var dtos = new List<UserDto>();
        
        foreach (var user in users)
        {
            var roles = await _repository.GetRolesAsync(user.Id, null, cancellationToken);
            dtos.Add(new UserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.IsActive,
                user.LastLoginAt,
                roles.ToList()
            ));
        }

        return Result<IReadOnlyList<UserDto>>.Ok(dtos);
    }
}

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashService _passwordHasher;
    private readonly ITenantContext _tenantContext;

    public CreateUserHandler(
        IUserRepository repository, 
        IUnitOfWork unitOfWork,
        IPasswordHashService passwordHasher,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _repository.GetByEmailAsync(request.User.Email, null, cancellationToken);
        if (existingUser != null)
            return Result<UserDto>.Fail("A user with this email already exists.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId.Value,
                UserName = request.User.Email,
                Email = request.User.Email,
                NormalizedEmail = request.User.Email.ToUpperInvariant(),
                FullName = request.User.FullName,
                PhoneNumber = request.User.PhoneNumber,
                EmailConfirmed = false,
                IsActive = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            user.PasswordHash = _passwordHasher.HashPassword(request.User.Password);

            await _repository.InsertAsync(user, _unitOfWork.Transaction, cancellationToken);

            var roleId = await _repository.GetRoleIdByNameAsync(request.User.Role, _tenantContext.TenantId.Value, _unitOfWork.Transaction, cancellationToken);
            if (roleId == Guid.Empty)
            {
                // Simple logic for missing role - in a real app, you might want to create it if it's tenant-specific.
                // Assuming it already exists globally or we fail.
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<UserDto>.Fail("Invalid role specified.");
            }

            await _repository.AssignRoleAsync(user.Id, roleId, _unitOfWork.Transaction, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = new UserDto(
                user.Id, user.UserName, user.Email, user.FullName, user.PhoneNumber, user.IsActive, user.LastLoginAt, new List<string> { request.User.Role }
            );

            return Result<UserDto>.Ok(dto, "User created successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UpdateUserHandler(IUserRepository repository, IUnitOfWork unitOfWork, ITenantContext tenantContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, null, cancellationToken);
        if (user == null)
            return Result.Fail("User not found.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            user.IsActive = request.User.IsActive;
            await _repository.UpdateAsync(user, _unitOfWork.Transaction, cancellationToken);

            // Fetch existing roles to update if necessary
            var currentRoles = await _repository.GetRolesAsync(user.Id, _unitOfWork.Transaction, cancellationToken);
            if (!currentRoles.Contains(request.User.Role))
            {
                // Remove old roles (simplified to assume 1 role for this scenario)
                var oldRoleId = await _repository.GetRoleIdByNameAsync(currentRoles.FirstOrDefault() ?? "", _tenantContext.TenantId.Value, _unitOfWork.Transaction, cancellationToken);
                if (oldRoleId != Guid.Empty)
                    await _repository.RemoveRoleAsync(user.Id, oldRoleId, _unitOfWork.Transaction, cancellationToken);

                // Assign new role
                var newRoleId = await _repository.GetRoleIdByNameAsync(request.User.Role, _tenantContext.TenantId.Value, _unitOfWork.Transaction, cancellationToken);
                if (newRoleId != Guid.Empty)
                    await _repository.AssignRoleAsync(user.Id, newRoleId, _unitOfWork.Transaction, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return Result.Ok("User updated successfully.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
