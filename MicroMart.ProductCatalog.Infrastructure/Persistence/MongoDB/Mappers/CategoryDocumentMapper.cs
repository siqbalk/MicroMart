using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Mappers;

public static class CategoryDocumentMapper
{
    public static CategoryDocument ToDocument(Category c) => new()
    {
        Id = c.Id.Value,
        Name = c.Name,
        Slug = c.Slug.Value,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        ParentId = c.ParentId?.Value,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };

    public static Category ToDomain(CategoryDocument d)
    {
        var id = CategoryId.Create(d.Id).Value;
        var slug = Slug.Create(d.Slug);
        var parentId = d.ParentId == null ? (CategoryId?)null
            : CategoryId.Create(d.ParentId).Value;

        return Category.Reconstitute(
            id, d.Name, d.Description, slug.Value,
            d.ImageUrl, parentId, d.DisplayOrder,
            d.IsActive, d.CreatedAt, d.UpdatedAt);
    }
}
