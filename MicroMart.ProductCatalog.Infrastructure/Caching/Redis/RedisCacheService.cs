using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Infrastructure.Caching.Redis;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<RedisSettings> options,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly RedisSettings _settings = options.Value;
    private readonly JsonSerializerOptions _json = new()
    { PropertyNameCaseInsensitive = true };

    private string PrefixKey(string key) => $"{_settings.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var prefixed = PrefixKey(key);
            var value = await _db.StringGetAsync(prefixed);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString()!, _json);
        }
        catch (Exception ex)
        {
            // Redis failure = cache miss, never crash the request
            logger.LogWarning(ex,
                "Redis GET failed for key '{Key}' — falling through to source", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value,
        int expirySeconds, CancellationToken ct)
    {
        try
        {
            var prefixed = PrefixKey(key);
            var serialized = JsonSerializer.Serialize(value, _json);
            var expiry = TimeSpan.FromSeconds(expirySeconds);
            await _db.StringSetAsync(prefixed, serialized, expiry);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Redis SET failed for key '{Key}' — value not cached", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        try
        {
            await _db.KeyDeleteAsync(PrefixKey(key));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis DELETE failed for key '{Key}'", key);
        }
    }

    // Pattern-based eviction — used after writes to invalidate related entries.
    // e.g. RemoveByPatternAsync("products:paged:*") clears all paged list caches.
    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct)
    {
        try
        {
            var prefixedPattern = PrefixKey(pattern);
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: prefixedPattern).ToArray();

            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);

            logger.LogDebug(
                "Evicted {Count} Redis keys matching pattern '{Pattern}'",
                keys.Length, pattern);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Redis pattern eviction failed for pattern '{Pattern}'", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try { return await _db.KeyExistsAsync(PrefixKey(key)); }
        catch { return false; }
    }
}