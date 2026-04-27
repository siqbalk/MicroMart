using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.Shared.Core.Results;
using System;

namespace MicroMart.ProductCatalog.Application.Products.Queries;
public sealed record GetProductsQuery(
    int Page,
    int PageSize,
    string? CategoryId = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    string? SortBy = null,
    bool Ascending = true)
    : IQuery<PagedResponse<ProductResponse>>;

public sealed class GetProductsHandler(IProductRepository repo)
    : IQueryHandler<GetProductsQuery, PagedResponse<ProductResponse>>
{
    public async Task<Result<PagedResponse<ProductResponse>>> Handle(
        GetProductsQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await repo.GetPagedAsync(
            query.Page, query.PageSize,
            query.CategoryId, query.IsActive, query.IsFeatured,
            query.SortBy, query.Ascending, ct);

        var responses = items.Adapt<IReadOnlyList<ProductResponse>>();

        var response = new PagedResponse<ProductResponse>
        {
            Items = responses,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        return Result.Success(response);

    }
}