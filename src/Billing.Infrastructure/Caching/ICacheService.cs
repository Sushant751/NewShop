using Billing.Application.Abstractions;

namespace Billing.Infrastructure.Caching;

/// <summary>
/// Cache options bound from configuration.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "billing";
    public int DefaultTtlSeconds { get; set; } = 300;
}
