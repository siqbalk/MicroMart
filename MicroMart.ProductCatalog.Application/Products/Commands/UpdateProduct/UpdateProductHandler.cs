using Mapster;
using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed class UpdateProductHandler(
    IProductRepository repo,
    IUnitOfWork uow,
    IProductSearchService search)
    : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(
        UpdateProductCommand cmd, CancellationToken ct)
    {
        var idResult = ProductId.Create(cmd.ProductId);
        if (idResult.IsFailure)
            return Result.Failure<ProductResponse>(idResult.Error);

        var product = await repo.FindByIdAsync(idResult.Value, ct);
        if (product is null)
            return Result.Failure<ProductResponse>(
                Error.NotFound("Product", cmd.ProductId));

        var updateResult = product.UpdateDetails(cmd.Name, cmd.Description);
        if (updateResult.IsFailure)
            return Result.Failure<ProductResponse>(updateResult.Error);

        if (cmd.Attributes is not null)
        {
            var attrResult = product.UpdateAttributes(cmd.Attributes);
            if (attrResult.IsFailure)
                return Result.Failure<ProductResponse>(attrResult.Error);
        }

        var dimsResult = Dimensions.Create(
            cmd.WeightKg, cmd.LengthCm, cmd.WidthCm, cmd.HeightCm);
        if (dimsResult.IsFailure)
            return Result.Failure<ProductResponse>(dimsResult.Error);
        product.UpdateDimensions(dimsResult.Value);

        await repo.UpdateAsync(product, ct);
        await uow.SaveChangesAsync(ct);
        await search.UpdateProductIndexAsync(product, ct);

        return Result.Success(product.Adapt<ProductResponse>());
    }
}