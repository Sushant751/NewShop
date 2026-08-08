using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Billing.API.Controllers;

/// <summary>
/// Base controller for all API endpoints. Provides shared access to the
/// MediatR sender and standard response helpers that wrap results in the
/// uniform API envelope.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator => _mediator ??=
        HttpContext.RequestServices.GetRequiredService<IMediator>();

    /// <summary>
    /// Converts a <see cref="Billing.Shared.Results.Result{T}"/> into an
    /// <see cref="IActionResult"/> with the appropriate HTTP status code.
    /// </summary>
    protected IActionResult ToActionResult<T>(Billing.Shared.Results.Result<T> result)
    {
        if (result.Success)
            return Ok(result);

        return result.Errors.Count > 0
            ? BadRequest(result)
            : UnprocessableEntity(result);
    }

    /// <summary>
    /// Converts a non-generic <see cref="Billing.Shared.Results.Result"/> into
    /// an <see cref="IActionResult"/>.
    /// </summary>
    protected IActionResult ToActionResult(Billing.Shared.Results.Result result)
    {
        if (result.Success)
            return Ok(result);

        return result.Errors.Count > 0
            ? BadRequest(result)
            : UnprocessableEntity(result);
    }

    /// <summary>
    /// Extracts the caller's IP address and user-agent for audit logging.
    /// </summary>
    protected string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    protected string? GetDeviceInfo() =>
        HttpContext.Request.Headers.UserAgent.ToString();
}
