using MediatR;
using MicroMart.ProductCatalog.Api.Extensions;
using MicroMart.ProductCatalog.Api.Requests.Categories;
using MicroMart.ProductCatalog.Application.Categories.Commands;
using MicroMart.ProductCatalog.Application.Categories.Queries;
using MicroMart.ProductCatalog.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MicroMart.ProductCatalog.Api.Endpoints.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/categories")
            .WithTags("Categories")
            .WithOpenApi();

        // GET /api/v1/categories
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllCategoriesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetAllCategories")
        .Produces<IReadOnlyList<CategoryResponse>>();          // ← CategoryResponse

        // GET /api/v1/categories/top-level
        group.MapGet("/top-level", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTopLevelCategoriesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetTopLevelCategories")
        .Produces<IReadOnlyList<CategoryResponse>>();          // ← CategoryResponse

        // GET /api/v1/categories/{id}
        group.MapGet("/{id}", async (                          // ← string, not Guid
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCategoryByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetCategoryById")
        .Produces<CategoryResponse>()                          // ← CategoryResponse
        .ProducesProblem(404);

        // GET /api/v1/categories/slug/{slug}
        group.MapGet("/slug/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCategoryBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetCategoryBySlug")
        .Produces<CategoryResponse>()                          // ← CategoryResponse
        .ProducesProblem(404);

        // POST /api/v1/categories → 201
        group.MapPost("/", async (
            [FromBody] CreateCategoryRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new CreateCategoryCommand(
                req.Name, req.Description, req.ParentId.ToString(), req.DisplayOrder);
            var result = await sender.Send(command, ct);
            return result.ToApiResult(dto =>
                TypedResults.Created($"/api/v1/categories/{dto.Id}", dto));
        })
        .WithName("CreateCategory")
        .Produces<CategoryResponse>(201)                       // ← CategoryResponse
        .ProducesValidationProblem()
        .ProducesProblem(409);

        // PUT /api/v1/categories/{id} → 204
        group.MapPut("/{id}", async (                          // ← string, not Guid
            string id,
            [FromBody] UpdateCategoryRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateCategoryCommand(id, req.Name, req.Description, req.ImageUrl, req.DisplayOrder), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCategory")
        .Produces(204)
        .ProducesProblem(404);

      

        // DELETE /api/v1/categories/{id} → 204
        group.MapDelete("/{id}", async (                       // ← string, not Guid
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCategoryCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteCategory")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409);

        return app;
    }
}