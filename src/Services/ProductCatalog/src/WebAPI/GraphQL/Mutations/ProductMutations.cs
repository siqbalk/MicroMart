
using global::MicroMart.ProductCatalog.Application.Products.Commands;
using global::MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;
using global::MicroMart.ProductCatalog.Application.Products.Commands.UpdateProduct;
using global::MicroMart.Shared.Core.Exceptions;
using HotChocolate;
using HotChocolate.Types;
using MediatR;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.Shared.Core.Results;
using System.ComponentModel.DataAnnotations;


namespace MicroMart.ProductCatalog.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ProductMutations
{
    // ── Create ──────────────────────────────────────────────────────────────
    [GraphQLDescription("Create a new product and return the created ProductDto.")]
    public async Task<ProductResponse> CreateProduct(
        CreateProductInput input,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new CreateProductCommand(
            input.Name, input.Description, input.Sku,
            input.PriceAmount, input.PriceCurrency,
            input.InitialStock, input.CategoryId.ToString(),
            input.WeightKg, input.LengthCm, input.WidthCm, input.HeightCm,
            input.Attributes), ct);
        return result.ThrowIfError();
    }

    // ── Update ──────────────────────────────────────────────────────────────
    [GraphQLDescription("Update a product's name and description.")]
    public async Task UpdateProduct(
        UpdateProductInput input,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new UpdateProductCommand(input.Id.ToString(), input.Name, input.Description), ct);
    }

    // ── Delete ──────────────────────────────────────────────────────────────
    [GraphQLDescription("Delete a product by Id.")]
    public async Task<bool> DeleteProduct(
        [GraphQLNonNullType] Guid id,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new DeleteProductCommand(id.ToString()), ct);
        result.ThrowIfError();
        return true;
    }

    // ── Price ───────────────────────────────────────────────────────────────
    [GraphQLDescription("Change the regular price of a product.")]
    public async Task<bool> ChangeProductPrice(
        ChangePriceInput input,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ChangeProductPriceCommand(input.ProductId.ToString(), input.Amount, input.Currency), ct);
        result.ThrowIfError();
        return true;
    }

    // ── Sale Price ──────────────────────────────────────────────────────────
    [GraphQLDescription("Set or remove a sale price. Pass null amount to remove.")]
    public async Task<bool> SetProductSalePrice(
        SetSalePriceInput input,
        ISender sender,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new SetSalePriceCommand(input.ProductId.ToString(), (decimal)input?.Amount, input.Currency), ct);
        result.ThrowIfError();
        return true;
    }

    // ── Stock ───────────────────────────────────────────────────────────────
    [GraphQLDescription("Add quantity to a product's stock.")]
    public async Task<bool> AddProductStock(
        [GraphQLNonNullType] Guid productId,
        int quantity,
        ISender sender,
        CancellationToken ct = default)
    {
        (await sender.Send(new AddStockCommand(productId.ToString(), quantity), ct)).ThrowIfError();
        return true;
    }

    [GraphQLDescription("Deduct quantity from a product's stock.")]
    public async Task<bool> DeductProductStock(
        [GraphQLNonNullType] Guid productId,
        int quantity,
        ISender sender,
        CancellationToken ct = default)
    {
        (await sender.Send(new DeductStockCommand(productId.ToString(), quantity), ct)).ThrowIfError();
        return true;
    }

    // ── Featured ────────────────────────────────────────────────────────────
    [GraphQLDescription("Set the featured status of a product.")]
    public async Task<bool> SetProductFeatured(
        [GraphQLNonNullType] Guid productId,
        bool isFeatured,
        ISender sender,
        CancellationToken ct = default)
    {
        (await sender.Send(new SetFeaturedCommand(productId.ToString(), isFeatured), ct)).ThrowIfError();
        return true;
    }
}

// ── Input types — Hot Chocolate generates these automatically ──────────────
public sealed record CreateProductInput(
    string Name, string Description, string Sku,
    decimal PriceAmount, string PriceCurrency,
    int InitialStock, Guid CategoryId,
    decimal WeightKg = 0,
    decimal LengthCm = 0,
    decimal WidthCm = 0,
    decimal HeightCm = 0,
    Dictionary<string, string>? Attributes = null);

public sealed record UpdateProductInput(Guid Id, string Name, string Description);
public sealed record ChangePriceInput(Guid ProductId, decimal Amount, string Currency);
public sealed record SetSalePriceInput(Guid ProductId, decimal? Amount, string? Currency);