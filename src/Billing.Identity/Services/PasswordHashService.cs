using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Billing.Identity.Services;

public sealed class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new User { Email = "local" }, password);
    }
}
