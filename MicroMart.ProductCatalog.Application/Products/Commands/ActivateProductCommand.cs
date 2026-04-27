using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record ActivateProductCommand(string ProductId)
    : ICommand<ProductResponse>;

public sealed class ActivateProductHandler(
    IProductRepository repo, IUnitOfWork uow,
    IProductSearchService search)
    : ICommandHandler<ActivateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        ActivateProductCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var result = product.Activate();
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        await search.IndexProductAsync(product, ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}
