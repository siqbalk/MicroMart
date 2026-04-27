using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands;
public sealed record RemoveProductImageCommand(string Id, string ImageUrl) : ICommand;

public sealed class RemoveProductImageCommandHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IImageStorageService storage
    //ICacheInvalidationService cache
    )
    : ICommandHandler<RemoveProductImageCommand>
{
    public async Task<Result> Handle(RemoveProductImageCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.Id);
        if (idResult.IsFailure) return Result.Failure(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("Product", cmd.Id));

        var removeResult = product.RemoveImage(cmd.ImageUrl);
        if (removeResult.IsFailure) return removeResult;

        await storage.DeleteAsync(cmd.ImageUrl, ct);
        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
       // await cache.InvalidateProductAsync(cmd.Id, product.Slug.Value, product.CategoryId.Value.ToString());

        return Result.Success();
    }
}