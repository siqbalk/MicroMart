using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Categories.Commands;
public sealed record DeleteCategoryCommand(string Id) : ICommand;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    IUnitOfWork uow
    //ICacheInvalidationService cache
    )
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand cmd, CancellationToken ct)
    {
        var idResult = CategoryId.Create(cmd.Id);
        if (idResult.IsFailure) return Result.Failure(idResult.Error);

        var category = await categoryRepo.FindByIdAsync(idResult.Value, ct);
        if (category is null)
            return Result.Failure(Error.NotFound("Category", cmd.Id));

        // Guard 1 — no subcategories
        var subCategories = await categoryRepo.GetSubCategoriesAsync(idResult.Value, ct);
        if (subCategories.Any())
            return Result.Failure(Error.Conflict(
                "Category",
                $"Cannot delete '{category.Name}' — it has {subCategories.Count} subcategory(ies). Delete or move them first."));

        // Guard 2 — no products assigned to this category
        var products = await productRepo.GetByCategoryAsync(idResult.Value, ct);
        if (products.Any())
            return Result.Failure(Error.Conflict(
                "Category",
                $"Cannot delete '{category.Name}' — it contains {products.Count} product(s). Reassign them first."));

        await categoryRepo.DeleteAsync(idResult.Value, ct);
        await uow.SaveChangesAsync(ct);
       // await cache.InvalidateCategoryAsync(cmd.Id, category.Slug.Value);

        return Result.Success();
    }
}
