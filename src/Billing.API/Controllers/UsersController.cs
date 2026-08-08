using Billing.Application.Commands.Users;
using Billing.Application.DTOs.Users;
using Billing.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Billing.API.Controllers;

[Authorize(Roles = "GlobalAdmin,ShopAdmin")]
public class UsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get all users for the current tenant.")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsersQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new user within the current tenant.")]
    [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        // If current user is ShopAdmin, they shouldn't be able to create GlobalAdmin or ShopAdmin.
        // We will do a basic check here for simplicity.
        if (User.IsInRole("ShopAdmin") && (request.Role == "GlobalAdmin" || request.Role == "ShopAdmin"))
        {
            return Forbid();
        }

        var result = await _mediator.Send(new CreateUserCommand(request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update an existing user (e.g. IsActive status or Role).")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (User.IsInRole("ShopAdmin") && (request.Role == "GlobalAdmin" || request.Role == "ShopAdmin"))
        {
            return Forbid();
        }

        var result = await _mediator.Send(new UpdateUserCommand(id, request), cancellationToken);
        return ToActionResult(result);
    }
}
