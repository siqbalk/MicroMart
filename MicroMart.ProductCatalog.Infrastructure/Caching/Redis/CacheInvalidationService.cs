using MicroMart.ProductCatalog.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Caching.Redis;

public sealed class CacheInvalidationService(ICacheService cache)
{
    // Called after: CreateProduct, UpdateProduct, Activate, Deactivate, SetFeatured,
    //               ChangePrice, SetSalePrice, RemoveSalePrice, AddStock, DeductStock,
    //               AddImage, RemoveImage, AddTag, RemoveTag, AddVariant
    public async Task InvalidateProductAsync(string productId, string slug,
        string categoryId, CancellationToken ct)
    {
        await Task.WhenAll(
            // Exact-key evictions
            cache.RemoveAsync($"product:id:{productId}", ct),
            cache.RemoveAsync($"product:slug:{slug}", ct),

            // Pattern evictions — every paged list and category list could be stale
            cache.RemoveByPatternAsync("products:paged:*", ct),
            cache.RemoveByPatternAsync("products:featured:*", ct),
            cache.RemoveAsync($"products:category:{categoryId}", ct),
            cache.RemoveByPatternAsync("products:lowstock:*", ct)
        );
    }

    // Called after: CreateCategory, UpdateCategory, DeleteCategory, MoveCategory
    public async Task InvalidateCategoryAsync(string categoryId, string slug,
        CancellationToken ct)
    {
        await Task.WhenAll(
            cache.RemoveAsync($"category:id:{categoryId}", ct),
            cache.RemoveAsync($"category:slug:{slug}", ct),
            cache.RemoveAsync($"category:tree:{categoryId}", ct),
            cache.RemoveAsync("categories:all", ct),
            cache.RemoveAsync("categories:toplevel", ct)
        );
    }
}