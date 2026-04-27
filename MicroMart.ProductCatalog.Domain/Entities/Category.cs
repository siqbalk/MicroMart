
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Primitives;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Domain.Entities;

public sealed class Category : AggregateRoot<CategoryId>
{
    private readonly List<Category> _subCategories = [];

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public CategoryId? ParentId { get; private set; }   // null = top-level
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<Category> SubCategories
        => _subCategories.AsReadOnly();

    public bool IsTopLevel => ParentId is null;

    private Category() { }

    public static Result<Category> Create(
        string name, string description,
        CategoryId? parentId = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(
                Error.Validation("Category", "Category name is required"));

        var slugResult = Slug.Create(name);
        if (slugResult.IsFailure) return Result.Failure<Category>(slugResult.Error);

        return Result.Success(new Category
        {
            Id = CategoryId.New(),
            Name = name.Trim(),
            Description = description.Trim(),
            Slug = slugResult.Value,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(
                Error.Validation("Category", "Name is required"));

        var slugResult = Slug.Create(name);
        if (slugResult.IsFailure) return Result.Failure(slugResult.Error);

        Name = name.Trim();
        Slug = slugResult.Value;
        return Result.Success();
    }

    public static Category Reconstitute(
        CategoryId id,
        string name,
        string description,
        Slug slug,
        string? imageUrl,
        CategoryId? parentId,
        int displayOrder,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Category
        {
            Id = id,
            Name = name,
            Description = description,
            Slug = slug,
            ImageUrl = imageUrl,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAt = createdAt
        };
    }

    public void SetSubCategories(IEnumerable<Category> subCategories)
    {
        _subCategories.Clear();
        _subCategories.AddRange(subCategories);
    }

}
