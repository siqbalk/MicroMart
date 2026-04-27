using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Categories.Queries;
// ── GetCategoryById ───────────────────────────────────────────────────────
public sealed record GetCategoryByIdQuery(string Id)
    : IQuery<CategoryResponse>, ICacheableQuery
{
    public string CacheKey => $"category:id:{Id}";
    public int CacheDurationSeconds { get; } = 600;
    public bool BypassCache { get; } = false;
}

public sealed class GetCategoryByIdHandler(ICategoryRepository repo)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(
        GetCategoryByIdQuery query, CancellationToken ct)
    {
        var idResult = CategoryId.Create(query.Id);
        if (idResult.IsFailure)
            return Result.Failure<CategoryResponse>(idResult.Error);

        // FindByIdAsync populates SubCategories with direct children
        var category = await repo.FindByIdAsync(idResult.Value, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound("Category", query.Id));

        return Result.Success(category.Adapt<CategoryResponse>());
    }
}

// ── GetCategoryBySlug ─────────────────────────────────────────────────────
public sealed record GetCategoryBySlugQuery(string Slug)
    : IQuery<CategoryResponse>, ICacheableQuery
{
    public string CacheKey => $"category:slug:{Slug}";
    public int CacheDurationSeconds { get; } = 600;
    public bool BypassCache { get; } = false;
}

public sealed class GetCategoryBySlugHandler(ICategoryRepository repo)
    : IQueryHandler<GetCategoryBySlugQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(
        GetCategoryBySlugQuery query, CancellationToken ct)
    {
        var category = await repo.FindBySlugAsync(query.Slug, ct);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound("Category", query.Slug));

        return Result.Success(category.Adapt<CategoryResponse>());
    }
}
