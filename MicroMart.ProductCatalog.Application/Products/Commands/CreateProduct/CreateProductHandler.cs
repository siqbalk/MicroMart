using Mapster;
using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Application.IntegrationEvents;
using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository repo,
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    IPublishEndpoint bus,
    IProductSearchService search,
    ILogger<CreateProductHandler> logger)
    : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        CreateProductCommand cmd, CancellationToken ct)
    {
        // ── Step 1: Verify category exists ────────────────────────────
        var catIdResult = CategoryId.Create(cmd.CategoryId);
        if (catIdResult.IsFailure)
            return Result.Failure<ProductResponse>(catIdResult.Error);

        var categoryExists = await categoryRepo
            .FindByIdAsync(catIdResult.Value, ct);
        if (categoryExists is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Category", cmd.CategoryId));

        // ── Step 2: Build value objects ────────────────────────────────
        var skuResult = Sku.Create(cmd.Sku);
        if (skuResult.IsFailure)
            return Result.Failure<ProductResponse>(skuResult.Error);

        var priceResult = Money.Create(cmd.Price, cmd.Currency);
        if (priceResult.IsFailure)
            return Result.Failure<ProductResponse>(priceResult.Error);

        var dimsResult = Dimensions.Create(
            cmd.WeightKg, cmd.LengthCm, cmd.WidthCm, cmd.HeightCm);
        if (dimsResult.IsFailure)
            return Result.Failure<ProductResponse>(dimsResult.Error);

        // ── Step 3: Create the domain aggregate ───────────────────────
        // All business rules enforced inside Product.Create()
        var productResult = Product.Create(
            cmd.Name,
            cmd.Description,
            skuResult.Value,
            priceResult.Value,
            cmd.InitialStock,
            catIdResult.Value,
            dimsResult.Value,
            cmd.Attributes);

        if (productResult.IsFailure)
            return Result.Failure<ProductResponse>(productResult.Error);

        var product = productResult.Value;

        // ── Step 4: Add tags ──────────────────────────────────────────
        if (cmd.Tags is not null)
        {
            foreach (var tag in cmd.Tags)
            {
                var tagResult = product.AddTag(tag);
                if (tagResult.IsFailure)
                    return Result.Failure<ProductResponse>(tagResult.Error);
            }
        }

        // ── Step 5: Persist to MongoDB ────────────────────────────────
        await repo.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        // ── Step 6: Index in Elasticsearch ───────────────────────────
        // After DB save — if this fails, product exists in DB
        // but not in search. UnitOfWork handles this atomically.
        await search.IndexProductAsync(product, ct);

        // ── Step 7: Publish integration event to RabbitMQ ────────────
        // Other services (Cart, Inventory) react to this
        await bus.Publish(new ProductCreatedIntegrationEvent(
            product.Id.Value,
            product.Name,
            product.Sku.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId.Value,
            product.Stock), ct);

        logger.LogInformation(
            "Product created: {ProductId} — {Name} — SKU: {Sku}",
            product.Id.Value, product.Name, product.Sku.Value);

        // ── Step 8: Map domain → DTO and return ──────────────────────
        return Result.Success(product.Adapt<ProductResponse>());
    }
}