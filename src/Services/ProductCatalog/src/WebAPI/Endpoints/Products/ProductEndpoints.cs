using MediatR;
using MicroMart.ProductCatalog.Api.Extensions;
using MicroMart.ProductCatalog.Api.Requests.Products;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Application.Products.Commands;
using MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;
using MicroMart.ProductCatalog.Application.Products.Queries;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace MicroMart.ProductCatalog.Api.Endpoints.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/products")
            .WithTags("Products")
            .WithOpenApi();

        // ── QUERIES ────────────────────────────────────────────────────────────

        // GET /api/v1/products
        group.MapGet("/", async (
            [AsParameters] GetProductsParams p,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetProductsQuery(p.Page, p.PageSize, p.CategoryId, p.IsActive, p.IsFeatured, p.SortBy, p.Ascending), ct);
            return result.ToApiResult();
        })
        .WithName("GetProducts")
        .Produces<PagedResponse<ProductResponse>>(); // ← ProductResponse not ProductDto

        // GET /api/v1/products/{id}
        group.MapGet("/{id}", async(
            string id,                // ← string not Guid — your ProductId.Create() takes string
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetProductById")
        .Produces<ProductResponse>()
        .ProducesProblem(404);

        // GET /api/v1/products/slug/{slug}
        group.MapGet("/slug/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetProductBySlug")
        .Produces<ProductResponse>()
        .ProducesProblem(404);



        // GET /api/v1/products/featured?count=8
        group.MapGet("/featured", async (
            [FromQuery] int count = 8,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetFeaturedProductsQuery(count), ct);
            return result.ToApiResult();
        })
        .WithName("GetFeaturedProducts")
        .Produces<IReadOnlyList<ProductResponse>>();

        // GET /api/v1/products/category/{categoryId}
        group.MapGet("/category/{categoryId}", async (
            string categoryId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProductsByCategoryQuery(categoryId), ct);
            return result.ToApiResult();
        })
        .WithName("GetProductsByCategory")
        .Produces<IReadOnlyList<ProductResponse>>();

       

        // ── COMMANDS ───────────────────────────────────────────────────────────

        // POST /api/v1/products
        group.MapPost("/", async (
            [FromBody] CreateProductRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateProductCommand(
                req.Name,
                req.Description,
                req.Sku,
                req.PriceAmount,
                req.PriceCurrency,
                req.InitialStock,
                req.CategoryId.ToString(),
                req.WeightKg,
                req.LengthCm,
                req.WidthCm,
                req.HeightCm,
                req.Attributes), ct);

            return result.ToApiResult(dto =>
                TypedResults.Created($"/api/v1/products/{dto.Id}", dto));
        })
        .WithName("CreateProduct")
        .Produces<ProductResponse>(201)
        .ProducesValidationProblem()
        .ProducesProblem(409);

        // PUT /api/v1/products/{id}
        group.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateProductRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new Application.Products.Commands.UpdateProduct.UpdateProductCommand(id, req.Name, req.Description), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateProduct")
        .Produces(204)
        .ProducesProblem(404);

        // DELETE /api/v1/products/{id}
        group.MapDelete("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProductCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteProduct")
        .Produces(204)
        .ProducesProblem(404);

        // PATCH /api/v1/products/{id}/price
        group.MapPatch("/{id}/price", async (
            string id,
            [FromBody] ChangePriceRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeProductPriceCommand(id, req.Amount, req.Currency), ct);
            return result.ToApiResult();
        })
        .WithName("ChangePrice")
        .Produces(204);

        // PUT /api/v1/products/{id}/sale-price
        group.MapPut("/{id}/sale-price", async (
            string id,
            [FromBody] SetSalePriceRequest? req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SetSalePriceCommand(id, (decimal)req?.Amount, req?.Currency), ct);
            return result.ToApiResult();
        })
        .WithName("SetSalePrice")
        .Produces(204);

        // POST /api/v1/products/{id}/stock/add
        group.MapPost("/{id}/stock/add", async (
            string id,
            [FromBody] StockRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AddStockCommand(id, req.Quantity), ct);
            return result.ToApiResult();
        })
        .WithName("AddStock")
        .Produces(204);

        // POST /api/v1/products/{id}/stock/deduct
        group.MapPost("/{id}/stock/deduct", async (
            string id,
            [FromBody] StockRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeductStockCommand(id, req.Quantity), ct);
            return result.ToApiResult();
        })
        .WithName("DeductStock")
        .Produces(204);

        // POST /api/v1/products/{id}/images  (multipart)
        group.MapPost("/{id}/images", async (
            string id,
            IFormFile file,
            [FromForm] string altText = "",
            [FromForm] bool isPrimary = false,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await sender.Send(
                new AddProductImageCommand(id, stream, file.FileName, file.ContentType, altText, isPrimary), ct);
            return result.ToApiResult();
        })
        .WithName("AddProductImage")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(204)
        .DisableAntiforgery();

        // DELETE /api/v1/products/{id}/images?url=...
        group.MapDelete("/{id}/images", async (
            string id,
            [FromQuery] string url,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveProductImageCommand(id, url), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveProductImage")
        .Produces(204)
        .ProducesProblem(404);

        // PATCH /api/v1/products/{id}/featured
        group.MapPatch("/{id}/featured", async (
            string id,
            [FromBody] SetFeaturedRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SetFeaturedCommand(id, req.IsFeatured), ct);
            return result.ToApiResult();
        })
        .WithName("SetFeatured")
        .Produces(204);

        return app;
    }
}

// Bound from query string via [AsParameters]
internal record GetProductsParams(
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 20,
    [FromQuery] string? CategoryId = null,
    [FromQuery] bool? IsActive = null,
    [FromQuery] bool? IsFeatured = null,
    [FromQuery] string? SortBy = null,
    [FromQuery] bool Ascending = true);