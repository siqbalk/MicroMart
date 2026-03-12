using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Queries;

public sealed record GetProductBySlugQuery(string Slug)
    : IQuery<ProductResponse>, ICacheableQuery
{
    public string CacheKey => $"product:slug:{Slug}";
    public int CacheDurationSeconds => 300;
    public bool BypassCache => false;
}

public sealed class GetProductBySlugHandler(IProductRepository repo)
    : IQueryHandler<GetProductBySlugQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        GetProductBySlugQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Slug))
            return Result.Failure<ProductResponse>(
                Error.Validation("Slug", "Slug cannot be empty"));

        var product = await repo.FindBySlugAsync(query.Slug, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", query.Slug));

        return Result.Success(product.Adapt<ProductResponse>());
    }
}

