using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.IntegrationEvents;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record DeleteProductCommand(string ProductId)
    : ICommand;  // returns plain Result — no value

public sealed class DeleteProductHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IPublishEndpoint bus,
    IProductSearchService search)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Result> Handle(
        DeleteProductCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure) return Result.Failure(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("Product", cmd.ProductId));

        await repo.DeleteAsync(idResult.Value, ct);
        await uow.SaveChangesAsync(ct);
        await search.RemoveFromIndexAsync(idResult.Value, ct);

        await bus.Publish(new ProductDeletedIntegrationEvent(
            product.Id.Value, product.Name), ct);

        return Result.Success();
    }
}
