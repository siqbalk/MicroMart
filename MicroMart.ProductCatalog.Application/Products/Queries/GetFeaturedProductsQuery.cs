using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Queries;

public sealed record GetFeaturedProductsQuery(int Count = 10)
    : IQuery<IReadOnlyList<ProductSummaryResponse>>, ICacheableQuery
{
    public string CacheKey => $"products:featured:{Count}";
    public int CacheDurationSeconds => 600; // 10 minutes
    public bool BypassCache => false;
}

public sealed class GetFeaturedProductsHandler(IProductRepository repo)
    : IQueryHandler<GetFeaturedProductsQuery, IReadOnlyList<ProductSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<ProductSummaryResponse>>> Handle(
        GetFeaturedProductsQuery query, CancellationToken ct)
    {
        var products = await repo.GetFeaturedAsync(query.Count, ct);
        var response = products
            .Adapt<IReadOnlyList<ProductSummaryResponse>>();

        return Result.Success(response);
    }
}
