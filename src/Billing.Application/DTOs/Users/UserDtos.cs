namespace Billing.Application.DTOs.Users;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    DateTime? LastLoginAt,
    List<string> Roles,
    string? TenantName = null
);

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    string Password,
    string Role
);

public sealed record UpdateUserRequest(
    bool IsActive,
    string Role
);
