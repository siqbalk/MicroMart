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

public sealed record AddProductImageCommand(
    string Id, Stream FileStream, string FileName,
    string ContentType, string AltText, bool IsPrimary)
    : ICommand;

public sealed class AddProductImageCommandHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IImageStorageService storage)
    : ICommandHandler<AddProductImageCommand>
{
    public async Task<Result> Handle(AddProductImageCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.Id);
        if (idResult.IsFailure) return Result.Failure(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("Product", cmd.Id));

        // Upload to Azure Blob — returns CDN URL
        var url = await storage.UploadAsync(cmd.FileStream, cmd.FileName, cmd.ContentType, ct);

        var addResult = product.AddImage(url, cmd.AltText, cmd.IsPrimary);
        if (addResult.IsFailure) return addResult;

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
       // await cache.InvalidateProductAsync(cmd.Id, product.Slug.Value, product.CategoryId.Value.ToString());

        return Result.Success();
    }
}