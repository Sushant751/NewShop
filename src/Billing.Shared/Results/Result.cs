namespace Billing.Shared.Results;

/// <summary>
/// Standard API response envelope used across all endpoints.
/// </summary>
/// <typeparam name="T">Type of the payload.</typeparam>
public sealed class Result<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static Result<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message ?? "Operation completed successfully." };

    public static Result<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };

    public static Result<T> Fail(IReadOnlyList<string> errors, string message = "Validation failed.") =>
        new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Non-generic result for operations that return no payload.
/// </summary>
public sealed class Result
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static Result Ok(string? message = null) =>
        new() { Success = true, Message = message ?? "Operation completed successfully." };

    public static Result Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };

    public static Result Fail(IReadOnlyList<string> errors, string message = "Validation failed.") =>
        new() { Success = false, Message = message, Errors = errors };
}
