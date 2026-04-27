using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Queries;


public sealed record GetProductsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? CategoryId = null,
    bool? IsFeatured = null,
    string? SortBy = null,
    bool Ascending = false
) : IQuery<PagedResponse<ProductSummaryResponse>>, ICacheableQuery
{
    public string CacheKey =>
        $"products:paged:{Page}:{PageSize}:{CategoryId}:{IsFeatured}:{SortBy}:{Ascending}";
    public int CacheDurationSeconds => 60; // 1 min — lists change more often
    public bool BypassCache => false;
}

public sealed class GetProductsPagedHandler(IProductRepository repo)
    : IQueryHandler<GetProductsPagedQuery,
        PagedResponse<ProductSummaryResponse>>
{
    public async Task<Result<PagedResponse<ProductSummaryResponse>>> Handle(
        GetProductsPagedQuery query, CancellationToken ct)
    {
        if (query.Page < 1)
            return Result.Failure<PagedResponse<ProductSummaryResponse>>(
                Error.Validation("Page", "Page must be greater than 0"));

        if (query.PageSize < 1 || query.PageSize > 100)
            return Result.Failure<PagedResponse<ProductSummaryResponse>>(
                Error.Validation("PageSize", "PageSize must be between 1 and 100"));

        var (items, total) = await repo.GetPagedAsync(
            query.Page, query.PageSize,
            query.CategoryId, isActive: true,
            query.IsFeatured, query.SortBy,
            query.Ascending, ct);

        var response = new PagedResponse<ProductSummaryResponse>
        {
            Items = items.Adapt<IReadOnlyList<ProductSummaryResponse>>(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };

        return Result.Success(response);
    }
}

