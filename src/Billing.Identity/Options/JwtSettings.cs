namespace Billing.Identity.Options;

/// <summary>
/// Strongly-typed configuration for JWT token generation and validation.
/// Bound from the "Jwt" configuration section.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BillingSystem";
    public string Audience { get; set; } = "BillingSystemClients";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
