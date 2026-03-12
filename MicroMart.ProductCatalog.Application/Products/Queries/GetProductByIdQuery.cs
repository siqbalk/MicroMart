using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Queries;

public sealed record GetProductByIdQuery(string Id)
    : IQuery<ProductResponse>, ICacheableQuery
{
    public string CacheKey => $"product:id:{Id}";
    public int CacheDurationSeconds => 300; // 5 minutes
    public bool BypassCache => false;
}

public sealed class GetProductByIdHandler(IProductRepository repo)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        GetProductByIdQuery query, CancellationToken ct)
    {
        var idResult = ProductId.Create(query.Id);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", query.Id));

        return Result.Success(product.Adapt<ProductResponse>());
    }
}

