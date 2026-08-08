namespace Billing.Shared.Exceptions;

/// <summary>
/// Base type for all domain-driven application exceptions.
/// </summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.", 404) { }
}

public sealed class ValidationException : AppException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }

    public ValidationException(string error)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = new[] { error };
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message, 401) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409) { }
}

public sealed class TenantContextMissingException : AppException
{
    public TenantContextMissingException()
        : base("Tenant context could not be resolved for the current request.", 400) { }
}
