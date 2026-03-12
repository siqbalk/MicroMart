using FluentValidation;
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

public sealed record SetSalePriceCommand(
    string ProductId,
    decimal SalePrice,
    string Currency
) : ICommand<ProductResponse>;

public sealed class SetSalePriceValidator
    : AbstractValidator<SetSalePriceCommand>
{
    public SetSalePriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("Sale price must be greater than zero");
        RuleFor(x => x.Currency)
            .NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
    }
}

public sealed class SetSalePriceHandler(
    IProductRepository repo, IUnitOfWork uow)
    : ICommandHandler<SetSalePriceCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        SetSalePriceCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var moneyResult = Money.Create(cmd.SalePrice, cmd.Currency);
        if (moneyResult.IsFailure)
            return Result.Failure<ProductResponse>(moneyResult.Error);

        var result = product.SetSalePrice(moneyResult.Value);
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}