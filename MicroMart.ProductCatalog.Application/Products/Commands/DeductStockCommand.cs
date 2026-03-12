using FluentValidation;
using Mapster;
using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Application.IntegrationEvents;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record DeductStockCommand(string ProductId, int Quantity)
    : ICommand<ProductResponse>;

public sealed class DeductStockValidator
    : AbstractValidator<DeductStockCommand>
{
    public DeductStockValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity to deduct must be greater than zero");
    }
}

public sealed class DeductStockHandler(
    IProductRepository repo, IUnitOfWork uow, IPublishEndpoint bus)
    : ICommandHandler<DeductStockCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        DeductStockCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var result = product.DeductStock(cmd.Quantity);
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);
        // ↑ returns "Insufficient stock" error back to Order saga

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        await bus.Publish(new StockUpdatedIntegrationEvent(
            product.Id.Value, product.Stock, cmd.Quantity, "DEDUCT"), ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}
