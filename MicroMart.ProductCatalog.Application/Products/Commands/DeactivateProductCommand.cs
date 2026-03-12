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

public sealed record DeactivateProductCommand(string ProductId)
    : ICommand<ProductResponse>;

public sealed class DeactivateProductHandler(
    IProductRepository repo, IUnitOfWork uow,
    IProductSearchService search)
    : ICommandHandler<DeactivateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        DeactivateProductCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var result = product.Deactivate();
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        await search.RemoveFromIndexAsync(idResult.Value, ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}
