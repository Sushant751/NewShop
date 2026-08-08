using Billing.Application.Commands.Purchases;
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
/// Supplier and purchase order management endpoints.
/// </summary>
[Authorize]
public class SuppliersController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = nameof(Permissions.PurchasesView))]
    [SwaggerOperation(Summary = "Get a paginated list of suppliers.")]
    [ProducesResponseType(typeof(Result<PagedResult<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSuppliersQuery(page, pageSize, search), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.PurchasesView))]
    [SwaggerOperation(Summary = "Get a supplier by id.")]
    [ProducesResponseType(typeof(Result<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplier([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSupplierByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissions.PurchasesCreate))]
    [SwaggerOperation(Summary = "Create a new supplier.")]
    [ProducesResponseType(typeof(Result<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SupplierDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateSupplierCommand(request), cancellationToken);
        return ToActionResult(result);
    }
}

/// <summary>
/// Purchase order endpoints.
/// </summary>
[Authorize]
public class PurchasesController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = nameof(Permissions.PurchasesView))]
    [SwaggerOperation(Summary = "Get a paginated list of purchase orders.")]
    [ProducesResponseType(typeof(Result<PagedResult<PurchaseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetPurchasesQuery(page, pageSize), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.PurchasesView))]
    [SwaggerOperation(Summary = "Get a purchase order by id.")]
    [ProducesResponseType(typeof(Result<PurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchase([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPurchaseByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissions.PurchasesCreate))]
    [SwaggerOperation(Summary = "Create a new purchase order (adds stock to products).")]
    [ProducesResponseType(typeof(Result<PurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PurchaseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreatePurchaseCommand(request), cancellationToken);
        return ToActionResult(result);
    }
}
