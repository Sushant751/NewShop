using Billing.Application.Commands.Sales;
using Billing.Application.Common;
using Billing.Application.DTOs.Sales;
using Billing.Identity.Authorization;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Point-of-Sale (POS) endpoints: create sales, cancel sales, query sale history.
/// </summary>
[Authorize]
public class SalesController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = nameof(Permissions.SalesCreate))]
    [SwaggerOperation(Summary = "Get a paginated list of sales, optionally filtered by date range.")]
    [ProducesResponseType(typeof(Result<PagedResult<SaleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSalesQuery(from, to, page, pageSize), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.SalesCreate))]
    [SwaggerOperation(Summary = "Get a sale (invoice) by id, including line items and payments.")]
    [ProducesResponseType(typeof(Result<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSaleByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissions.SalesCreate))]
    [SwaggerOperation(Summary = "Create a new sale (POS checkout). Deducts stock and records payments.")]
    [ProducesResponseType(typeof(Result<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SaleDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateSaleCommand(request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = nameof(Permissions.SalesCancel))]
    [SwaggerOperation(Summary = "Cancel a sale, restoring stock. Requires a reason.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, [FromBody] CancelSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CancelSaleCommand(id, request?.Reason), cancellationToken);
        return ToActionResult(result);
    }
}
