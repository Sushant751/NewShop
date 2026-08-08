using Billing.Application.Commands.Products;
using Billing.Application.Common;
using Billing.Application.DTOs.Products;
using Billing.Identity.Authorization;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Product catalog CRUD, search, and low-stock queries.
/// </summary>
[Authorize]
public class ProductsController : BaseApiController
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get a paginated list of products.")]
    [ProducesResponseType(typeof(Result<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetProductsQuery(page, pageSize, search, orderBy, ascending), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a single product by id.")]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissions.ProductsCreate))]
    [SwaggerOperation(Summary = "Create a new product.")]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateProductCommand(request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.ProductsEdit))]
    [SwaggerOperation(Summary = "Update an existing product.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateProductCommand(id, request), cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = nameof(Permissions.ProductsDelete))]
    [SwaggerOperation(Summary = "Soft-delete a product.")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = nameof(Permissions.ProductsView))]
    [SwaggerOperation(Summary = "Search products by name, SKU, or barcode.")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new SearchProductsQuery(term, limit), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("low-stock")]
    [Authorize(Policy = nameof(Permissions.InventoryView))]
    [SwaggerOperation(Summary = "Get products at or below their reorder level.")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLowStockProductsQuery(), cancellationToken);
        return ToActionResult(result);
    }
}
