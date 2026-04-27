using FluentValidation;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Categories.Commands;
public sealed record UpdateCategoryCommand(
    string Id,
    string Name,
    string Description,
    string? ImageUrl = null,
    int DisplayOrder = 0)
    : ICommand;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        When(x => x.ImageUrl is not null, () =>
            RuleFor(x => x.ImageUrl).Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("ImageUrl must be a valid absolute URL."));
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository repo,
    IUnitOfWork uow
    //ICacheInvalidationService cache
    )
    : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var idResult = CategoryId.Create(cmd.Id);
        if (idResult.IsFailure) return Result.Failure(idResult.Error);

        var category = await repo.FindByIdAsync(idResult.Value, ct);
        if (category is null)
            return Result.Failure(Error.NotFound("Category", cmd.Id));

      //  var updateResult = category.Update(cmd.Name, cmd.Description, cmd.ImageUrl, cmd.DisplayOrder);
       // if (updateResult.IsFailure) return updateResult;

        await repo.UpdateAsync(category, ct);
        await uow.SaveChangesAsync(ct);
      //  await cache.InvalidateCategoryAsync(cmd.Id, category.Slug.Value);

        return Result.Success();
    }
}