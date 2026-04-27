using MediatR;
using MicroMart.ProductCatalog.Application.Categories.Queries;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.Shared.Core.Results;
using HotChocolate;

namespace MicroMart.ProductCatalog.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class CategoryQueries
{
    [GraphQLDescription("All categories as a flat list.")]
    [UseFiltering]
    public async Task<IReadOnlyList<CategoryResponse>> Categories(
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAllCategoriesQuery(), ct);
        return result.ThrowIfError();
    }

    [GraphQLDescription("Top-level categories only (no parent).")]
    public async Task<IReadOnlyList<CategoryResponse>> TopLevelCategories(
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTopLevelCategoriesQuery(), ct);
        return result.ThrowIfError();
    }

    [GraphQLDescription("Get a category by Id, including its direct subcategories.")]
    public async Task<CategoryResponse> CategoryById(
        ISender sender,
        [GraphQLNonNullType] Guid id,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetCategoryByIdQuery(id.ToString()), ct);

        return result.ThrowIfError();
    }

    [GraphQLDescription("Get a category by URL slug.")]
    public async Task<CategoryResponse> CategoryBySlug(
        ISender sender,
        string slug,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetCategoryBySlugQuery(slug), ct);

        return result.ThrowIfError();
    }
}