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


public sealed record SearchProductsQuery(
    string SearchTerm,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResponse<ProductSummaryResponse>>;
// ↑ does NOT implement ICacheableQuery — search is not cached

public sealed class SearchProductsHandler(
    IProductSearchService search,
    IProductRepository repo)
    : IQueryHandler<SearchProductsQuery,
        PagedResponse<ProductSummaryResponse>>
{
    public async Task<Result<PagedResponse<ProductSummaryResponse>>> Handle(
        SearchProductsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
            return Result.Failure<PagedResponse<ProductSummaryResponse>>(
                Error.Validation("SearchTerm", "Search term is required"));

        // Step 1: Elasticsearch returns matching ProductIds
        var searchResult = await search.SearchAsync(
            query.SearchTerm, query.Page, query.PageSize,null, ct);

        var idList = searchResult.ProductIds.ToList();
        if (!idList.Any())
            return Result.Success(new PagedResponse<ProductSummaryResponse>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = 0
            });

        // Step 2: Fetch full product data from MongoDB by those IDs
        var productIdObjects = idList
            .Select(id => ProductId.Create(id))
            .Where(r => r.IsSuccess)
            .Select(r => r.Value)
            .ToList();

        var products = await repo.GetByIdsAsync(productIdObjects, ct);

        var response = new PagedResponse<ProductSummaryResponse>
        {
            Items = products.Adapt<IReadOnlyList<ProductSummaryResponse>>(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = idList.Count
        };

        return Result.Success(response);
    }
}