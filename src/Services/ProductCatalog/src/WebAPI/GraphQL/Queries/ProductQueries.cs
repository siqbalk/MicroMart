using HotChocolate;
using HotChocolate.Types;
using MediatR;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Application.Products.Queries;
using MicroMart.Shared.Core.Exceptions;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProductQueries
{
    /// Returns a paged list of products with optional filters.
    [GraphQLDescription("Paged list of products with optional filters.")]
    public async Task<PagedResponse<ProductResponse>> Products(
     [Service] ISender sender,
     ProductFilterInput? filter = null,
     ProductSortInput? sort = null,
     int page = 1,
     int pageSize = 20,
     CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetProductsQuery(
                page,
                pageSize,
                filter?.CategoryId,
                filter?.IsActive,
                filter?.IsFeatured,
                sort?.SortBy,
                sort?.Ascending ?? true
            ),
            ct);

        return result.ThrowIfError();
    }

    /// Returns a single product by its Guid Id.
    [GraphQLDescription("Get a product by its Guid Id.")]
    public async Task<ProductResponse> ProductById(
        ISender sender,
        [GraphQLNonNullType] Guid id,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProductByIdQuery(id.ToString()), ct);
        return result.ThrowIfError();
    }

    /// Returns a product by its URL slug.
    [GraphQLDescription("Get a product by its URL slug.")]
    public async Task<ProductResponse> ProductBySlug(
        ISender sender,
        string slug,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProductBySlugQuery(slug), ct);
        return result.ThrowIfError();
    }

    /// Full-text search via Elasticsearch.
    [GraphQLDescription("Full-text product search powered by Elasticsearch.")]
    public async Task<PagedResponse<ProductSummaryResponse>> SearchProducts(
        ISender sender,
        string q = "",
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new SearchProductsQuery(q, page, pageSize), ct);
        return result.ThrowIfError();
    }

 

    /// Returns products marked as featured.
    [GraphQLDescription("Get featured products.")]
    public async Task<IReadOnlyList<ProductSummaryResponse>> FeaturedProducts(
        ISender sender,
        int count = 8,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetFeaturedProductsQuery(count), ct);
        return result.ThrowIfError();
    }

    /// Returns all products in a given category.
    [GraphQLDescription("Get all products belonging to a category.")]
    public async Task<IReadOnlyList<ProductSummaryResponse>> ProductsByCategory(
        ISender sender,
        [GraphQLNonNullType] Guid categoryId,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProductsByCategoryQuery(categoryId.ToString()), ct);
        return result.ThrowIfError();
    }

    
}