using Billing.Application.Commands.Sales;
using Billing.Application.DTOs;
using Billing.Identity.Authorization;
using Billing.Shared.Enums;
using Billing.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Billing.API.Controllers;

/// <summary>
/// Dashboard and reporting endpoints: summary metrics, top products,
/// daily sales trends, profit & loss.
/// </summary>
[Authorize]
public class DashboardController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = nameof(Permissions.ReportsView))]
    [SwaggerOperation(Summary = "Get the tenant dashboard summary (sales, purchases, profit, counts, top products, daily sales).")]
    [ProducesResponseType(typeof(Result<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDashboardQuery(from, to), cancellationToken);
        return ToActionResult(result);
    }
}
