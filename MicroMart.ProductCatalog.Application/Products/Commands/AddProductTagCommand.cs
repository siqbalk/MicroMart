using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record AddProductTagCommand(string ProductId, string Tag)
    : ICommand<ProductResponse>;

public sealed class AddProductTagHandler(
    IProductRepository repo, IUnitOfWork uow)
    : ICommandHandler<AddProductTagCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        AddProductTagCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var result = product.AddTag(cmd.Tag);
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(product.Adapt<ProductResponse>());
    }
}