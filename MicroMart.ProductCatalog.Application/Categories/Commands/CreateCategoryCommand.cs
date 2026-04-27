using FluentValidation;
using Mapster;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Categories.Commands;


public sealed record CreateCategoryCommand(
    string Name,
    string Description,
    string? ParentCategoryId = null,
    int DisplayOrder = 0
) : ICommand<CategoryResponse>;

public sealed class CreateCategoryValidator
    : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator(ICategoryRepository repo)
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(100)
            .MustAsync(async (name, ct) =>
            {
                var slugResult = Slug.Create(name);
                if (slugResult.IsFailure) return false;
                return !await repo.ExistsBySlugAsync(slugResult.Value.Value, ct);
            })
            .WithMessage(x => $"Category '{x.Name}' already exists");
    }
}

public sealed class CreateCategoryHandler(
    ICategoryRepository repo, IUnitOfWork uow)
    : ICommandHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(
        CreateCategoryCommand cmd, CancellationToken ct)
    {
        CategoryId? parentId = null;
        if (cmd.ParentCategoryId is not null)
        {
            var parentResult = CategoryId.Create(cmd.ParentCategoryId);
            if (parentResult.IsFailure)
                return Result.Failure<CategoryResponse>(parentResult.Error);
            parentId = parentResult.Value;
        }

        var result = Category.Create(
            cmd.Name, cmd.Description, parentId, cmd.DisplayOrder);
        if (result.IsFailure)
            return Result.Failure<CategoryResponse>(result.Error);

        await repo.AddAsync(result.Value, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(result.Value.Adapt<CategoryResponse>());
    }
}
