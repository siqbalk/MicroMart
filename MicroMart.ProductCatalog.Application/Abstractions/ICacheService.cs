using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface ICacheService
{
    /// Gets a cached value. Returns null if not found or expired.
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// Stores a value with explicit TTL in seconds.
    Task SetAsync<T>(string key, T value, int expirySeconds,
        CancellationToken ct = default);

    /// Removes a specific key.
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// Removes all keys matching a prefix pattern (e.g. "product:*").
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);

    /// Checks if a key exists in cache.
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}