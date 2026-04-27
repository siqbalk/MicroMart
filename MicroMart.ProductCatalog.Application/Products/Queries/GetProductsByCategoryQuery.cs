using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Queries;


public sealed record GetProductsByCategoryQuery(string CategoryId)
    : IQuery<IReadOnlyList<ProductSummaryResponse>>, ICacheableQuery
{
    public string CacheKey => $"products:category:{CategoryId}";
    public int CacheDurationSeconds => 120;
    public bool BypassCache => false;
}

public sealed class GetProductsByCategoryHandler(IProductRepository repo)
    : IQueryHandler<GetProductsByCategoryQuery,
        IReadOnlyList<ProductSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<ProductSummaryResponse>>> Handle(
        GetProductsByCategoryQuery query, CancellationToken ct)
    {
        var catIdResult = CategoryId.Create(query.CategoryId);
        if (catIdResult.IsFailure)
            return Result.Failure<IReadOnlyList<ProductSummaryResponse>>(catIdResult.Error);

        var products = await repo.GetByCategoryAsync(catIdResult.Value, ct);
        return Result.Success(
            products.Adapt<IReadOnlyList<ProductSummaryResponse>>());
    }
}