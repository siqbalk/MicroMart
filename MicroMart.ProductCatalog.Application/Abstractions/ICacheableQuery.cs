using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface ICacheableQuery
{
    // Unique cache key for this specific query + its parameters
    // Example: "product:id:abc123" or "products:paged:1:20:electronics"
    string CacheKey { get; }

    // How long to cache in seconds
    // 300 = 5 minutes for product detail
    // 60  = 1 minute for paginated lists (changes more often)
    int CacheDurationSeconds { get; }

    // Set to true for admin queries — bypasses cache entirely
    bool BypassCache { get; }
}