using Billing.Application.Commands.Customers;
using Billing.Application.Common;
using Billing.Application.DTOs;
using Billing.Identity.Authorization;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Customer management endpoints.
/// </summary>
[Authorize]
public class CustomersController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = nameof(Permissions.CustomersView))]
    [SwaggerOperation(Summary = "Get a paginated list of customers.")]
    [ProducesResponseType(typeof(Result<PagedResult<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetCustomersQuery(page, pageSize, search), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.CustomersView))]
    [SwaggerOperation(Summary = "Get a customer by id.")]
    [ProducesResponseType(typeof(Result<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissions.CustomersCreate))]
    [SwaggerOperation(Summary = "Create a new customer.")]
    [ProducesResponseType(typeof(Result<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CustomerDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateCustomerCommand(request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.CustomersEdit))]
    [SwaggerOperation(Summary = "Update an existing customer.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer([FromRoute] Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateCustomerCommand(id, request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.CustomersDelete))]
    [SwaggerOperation(Summary = "Soft-delete a customer.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteCustomerCommand(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = nameof(Permissions.CustomersView))]
    [SwaggerOperation(Summary = "Search customers by name, email, or phone.")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new SearchCustomersQuery(term, limit), cancellationToken);
        return ToActionResult(result);
    }
}
