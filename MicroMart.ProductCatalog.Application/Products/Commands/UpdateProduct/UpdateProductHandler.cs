using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IProductSearchService search)
    : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        UpdateProductCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.Id);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.Id));

        var updateResult = product.UpdateDetails(cmd.Name, cmd.Description);
        if (updateResult.IsFailure)
            return Result.Failure<ProductResponse>(updateResult.Error);


        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        await search.UpdateProductIndexAsync(product, ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}