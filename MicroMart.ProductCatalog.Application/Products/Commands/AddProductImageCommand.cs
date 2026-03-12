using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record AddProductImageCommand(
    string ProductId, string ImageUrl,
    string AltText, bool IsPrimary = false
) : ICommand<ProductResponse>;

public sealed class AddProductImageHandler(
    IProductRepository repo, IUnitOfWork uow)
    : ICommandHandler<AddProductImageCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        AddProductImageCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var result = product.AddImage(cmd.ImageUrl, cmd.AltText, cmd.IsPrimary);
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}