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

public sealed record ChangeProductPriceCommand(
    string ProductId,
    decimal NewPrice,
    string Currency
) : ICommand<ProductResponse>;

public sealed class ChangeProductPriceValidator
    : AbstractValidator<ChangeProductPriceCommand>
{
    public ChangeProductPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
    }
}

public sealed class ChangeProductPriceHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IPublishEndpoint bus)
    : ICommandHandler<ChangeProductPriceCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        ChangeProductPriceCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var priceResult = Money.Create(cmd.NewPrice, cmd.Currency);
        if (priceResult.IsFailure)
            return Result.Failure<ProductResponse>(priceResult.Error);

        var changePriceResult = product.ChangePrice(priceResult.Value);
        if (changePriceResult.IsFailure)
            return Result.Failure<ProductResponse>(changePriceResult.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        await bus.Publish(new ProductPriceChangedIntegrationEvent(
            product.Id.Value,
            priceResult.Value.Amount,
            priceResult.Value.Currency), ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}