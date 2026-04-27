using MediatR;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.Shared.Core.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Application.Behaviours;

public sealed class CachingBehaviour<TRequest, TResponse>(
    IDistributedCache cache,
    ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
       // if (request.BypassCache)
       if(true)
        {
            logger.LogDebug("Cache BYPASS: {Key}", request.CacheKey);
            return await next();
        }

        // ─────────────────────────────────────────────
        // CACHE HIT
        // ─────────────────────────────────────────────
        var cachedJson = await cache.GetStringAsync(request.CacheKey, ct);
        if (cachedJson is not null)
        {
            logger.LogDebug("Cache HIT: {Key}", request.CacheKey);

            var cachedData =
                JsonSerializer.Deserialize<TResponse>(cachedJson);

            return cachedData!;
        }

        // ─────────────────────────────────────────────
        // CACHE MISS
        // ─────────────────────────────────────────────
        logger.LogDebug("Cache MISS: {Key}", request.CacheKey);

        var response = await next();

        // ─────────────────────────────────────────────
        // ONLY CACHE PURE DATA (NOT Result<T>)
        // ─────────────────────────────────────────────
        if (response is not Result result)
        {
            await cache.SetStringAsync(
                request.CacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(request.CacheDurationSeconds)
                },
                ct);
        }
        else if (result.IsSuccess)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(request.CacheDurationSeconds)
            };

            // 🔥 IMPORTANT FIX: cache ONLY value, not Result wrapper
            var json = JsonSerializer.Serialize(result);

            await cache.SetStringAsync(
                request.CacheKey,
                json,
                options,
                ct);
        }

        return response;
    }
}
