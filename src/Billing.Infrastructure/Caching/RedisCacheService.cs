using System.Text.Json;
using Billing.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Billing.Infrastructure.Caching;

/// <summary>
/// Redis-backed distributed cache. Falls back to an in-memory cache when Redis
/// is unavailable so the application remains functional in development.
/// </summary>
public sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly CacheOptions _options;
    private readonly IMemoryCache _fallback;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnectionMultiplexer? _redis;
    private bool _redisUnavailable;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IOptions<CacheOptions> options, ILogger<RedisCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _fallback = new MemoryCache(new MemoryCacheOptions());
    }

    private async Task<IDatabase?> GetDatabaseAsync()
    {
        if (_redisUnavailable) return null;
        if (_redis is not null) return _redis.GetDatabase();

        await _connectionLock.WaitAsync();
        try
        {
            if (_redis is not null) return _redis.GetDatabase();
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                _redisUnavailable = true;
                return null;
            }
            _redis = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString);
            return _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection failed; falling back to in-memory cache.");
            _redisUnavailable = true;
            return null;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        if (db is not null)
        {
            var value = await db.StringGetAsync(key);
            if (value.HasValue) return JsonSerializer.Deserialize<T>(value!);
            return default;
        }
        return _fallback.Get<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var ttl = absoluteExpiration ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
        var db = await GetDatabaseAsync();
        if (db is not null)
        {
            var serialized = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, serialized, ttl);
            return;
        }
        _fallback.Set(key, value, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        if (db is not null)
        {
            await db.KeyDeleteAsync(key);
            return;
        }
        _fallback.Remove(key);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var db = await GetDatabaseAsync();
        if (db is null)
        {
            // Memory cache does not support pattern eviction; clear all.
            if (_fallback is MemoryCache mc)
            {
                mc.Compact(1.0);
            }
            return;
        }
        var server = _redis!.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"{_options.InstanceName}*{pattern}*").WithCancellation(cancellationToken))
        {
            await db.KeyDeleteAsync(key);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
        _fallback.Dispose();
        _connectionLock.Dispose();
    }
}
