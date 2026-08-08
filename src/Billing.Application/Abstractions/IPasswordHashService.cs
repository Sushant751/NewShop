namespace Billing.Application.Abstractions;

public interface IPasswordHashService
{
    string HashPassword(string password);
}
