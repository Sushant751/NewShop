using Billing.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Billing.Identity.Passwords;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2 password hasher so the application
/// layer can hash and verify passwords without depending on Identity types.
/// </summary>
public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password)
    {
        // A transient User instance is only used to satisfy the generic API.
        return _hasher.HashPassword(new User { Email = "local" }, password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var result = _hasher.VerifyHashedPassword(new User { Email = "local" }, hash, password);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
