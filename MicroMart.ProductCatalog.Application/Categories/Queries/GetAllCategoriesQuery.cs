using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Categories.Queries;

public sealed record GetAllCategoriesQuery()
    : IQuery<IReadOnlyList<CategoryResponse>>, ICacheableQuery
{
    public string CacheKey => "categories:all";
    public int CacheDurationSeconds => 3600; // 1 hour — categories rarely change
    public bool BypassCache => false;
}

public sealed class GetAllCategoriesHandler(ICategoryRepository repo)
    : IQueryHandler<GetAllCategoriesQuery,
        IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        GetAllCategoriesQuery query, CancellationToken ct)
    {
        var categories = await repo.GetAllAsync(ct);
        return Result.Success(
            categories.Adapt<IReadOnlyList<CategoryResponse>>());
    }
}


public sealed record GetTopLevelCategoriesQuery()
    : IQuery<IReadOnlyList<CategoryResponse>>, ICacheableQuery
{
    public string CacheKey => "categories:toplevel";
    public int CacheDurationSeconds { get; } = 600;
    public bool BypassCache { get; } = false;
}

public sealed class GetTopLevelCategoriesHandler(ICategoryRepository repo)
    : IQueryHandler<GetTopLevelCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> Handle(
        GetTopLevelCategoriesQuery query, CancellationToken ct)
    {
        var categories = await repo.GetTopLevelAsync(ct);
        return Result.Success(categories.Adapt<IReadOnlyList<CategoryResponse>>());
    }
}