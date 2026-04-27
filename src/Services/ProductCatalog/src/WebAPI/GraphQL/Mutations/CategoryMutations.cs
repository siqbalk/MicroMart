using MediatR;
using MicroMart.ProductCatalog.Application.Categories.Commands;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.Shared.Core.Exceptions;
using MicroMart.Shared.Core.Results;
using System.ComponentModel.DataAnnotations;

namespace MicroMart.ProductCatalog.Api.GraphQL.Mutations;
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class CategoryMutations
{
    [GraphQLDescription("Create a new category.")]
    public async Task<CategoryResponse> CreateCategory(
        CreateCategoryInput input, ISender sender, CancellationToken ct = default)
    {
        var result = await sender.Send(
            new CreateCategoryCommand(input.Name, input.Description, input.ParentId.ToString(), input.DisplayOrder), ct);
        return result.ThrowIfError();
    }

    [GraphQLDescription("Update a category's name, description, image, or display order.")]
    public async Task<bool> UpdateCategory(
        UpdateCategoryInput input, ISender sender, CancellationToken ct = default)
    {
        var result = await sender.Send(
            new UpdateCategoryCommand(input.Id.ToString(), input.Name, input.Description, input.ImageUrl, input.DisplayOrder), ct);
        result.ThrowIfError();
        return true;
    }



    [GraphQLDescription("Delete a category. Fails if it has products or subcategories.")]
    public async Task<bool> DeleteCategory(
        [GraphQLNonNullType] Guid id,
        ISender sender,
        CancellationToken ct = default)
    {
        (await sender.Send(new DeleteCategoryCommand(id.ToString()), ct)).ThrowIfError();
        return true;
    }
}

// ── Category input types ──────────────────────────────────────────────────
public sealed record CreateCategoryInput(
    string Name, string Description, Guid? ParentId = null, int DisplayOrder = 0);

public sealed record UpdateCategoryInput(
    Guid Id, string Name, string Description, string? ImageUrl = null, int DisplayOrder = 0);