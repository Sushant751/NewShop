using Billing.Application.DTOs.Users;
using Billing.Shared.Results;
using MediatR;
using System.Collections.Generic;

namespace Billing.Application.Commands.Users;

public sealed record CreateUserCommand(CreateUserRequest User) : IRequest<Result<UserDto>>;
public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest User) : IRequest<Result>;

public sealed record GetUsersQuery(bool IncludeAllTenants = false) : IRequest<Result<IReadOnlyList<UserDto>>>;
