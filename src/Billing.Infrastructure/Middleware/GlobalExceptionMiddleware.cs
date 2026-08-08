using System.Net;
using System.Text.Json;
using Billing.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Billing.Infrastructure.Middleware;

/// <summary>
/// Global exception handler. Translates domain exceptions into the standard
/// API response envelope and never leaks stack traces to the client.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, ve.Message, (IReadOnlyList<string>)ve.Errors),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, Array.Empty<string>()),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message, Array.Empty<string>()),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message, Array.Empty<string>()),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, Array.Empty<string>()),
            TenantContextMissingException => (StatusCodes.Status400BadRequest, exception.Message, Array.Empty<string>()),
            AppException => (StatusCodes.Status400BadRequest, exception.Message, Array.Empty<string>()),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access.", Array.Empty<string>()),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.", Array.Empty<string>())
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception. Path: {Path}. Message: {Message}",
                context.Request.Path, exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled application exception ({StatusCode}). Path: {Path}",
                statusCode, context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new
        {
            success = false,
            message,
            data = (object?)null,
            errors = errors.Count > 0 ? errors : null,
            traceId = context.TraceIdentifier,
            // Only include technical details in Development to aid debugging.
            detail = _env.IsDevelopment() && statusCode >= 500 ? exception.ToString() : null
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
