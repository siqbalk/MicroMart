using MediatR;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Primitives;
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
        // Admin bypass — always fresh data
        if (request.BypassCache)
        {
            logger.LogDebug("Cache BYPASS: {Key}", request.CacheKey);
            return await next();
        }

        // Check Redis
        var cachedJson = await cache.GetStringAsync(request.CacheKey, ct);
        if (cachedJson is not null)
        {
            logger.LogDebug("Cache HIT: {Key}", request.CacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedJson)!;
        }

        // Cache MISS — run the handler
        logger.LogDebug("Cache MISS: {Key}", request.CacheKey);
        var response = await next();

        // Only cache successful results — don't cache errors
        var isSuccess = response is Result result && result.IsSuccess;
        if (isSuccess)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(request.CacheDurationSeconds)
            };
            await cache.SetStringAsync(
                request.CacheKey,
                JsonSerializer.Serialize(response),
                options, ct);
        }

        return response;
    }
}
