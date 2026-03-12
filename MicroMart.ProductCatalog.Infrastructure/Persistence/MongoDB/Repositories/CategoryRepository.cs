using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Mappers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Repositories;

public sealed class CategoryRepository(MongoDbContext context) : ICategoryRepository
{
    private readonly IMongoCollection<CategoryDocument> _col = context.Categories;

    public async Task<Category?> FindByIdAsync(CategoryId id, CancellationToken ct)
    {
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Id, id.Value);
        var doc = await _col.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : CategoryDocumentMapper.ToDomain(doc);
    }

    public async Task<Category?> FindBySlugAsync(string slug, CancellationToken ct)
    {
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Slug, slug);
        var doc = await _col.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : CategoryDocumentMapper.ToDomain(doc);
    }

    public async Task<Category?> FindByIdWithSubcategoriesAsync(
        CategoryId id, CancellationToken ct)
    {
        // Fetch the category itself
        var category = await FindByIdAsync(id, ct);
        if (category == null) return null;

        // Fetch all direct sub-categories
        var subDocs = await _col
            .Find(Builders<CategoryDocument>.Filter.Eq(c => c.ParentId, id.Value))
            .ToListAsync(ct);

        var subCategories = subDocs.Select(CategoryDocumentMapper.ToDomain).ToList();
        category.SetSubCategories(subCategories);
        return category;
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct)
    {
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Slug, slug);
        return await _col.Find(filter).AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
    {
        var sort = Builders<CategoryDocument>.Sort.Ascending(c => c.DisplayOrder);
        var docs = await _col.Find(Builders<CategoryDocument>.Filter.Empty)
            .Sort(sort).ToListAsync(ct);
        return docs.Select(CategoryDocumentMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Category>> GetTopLevelAsync(CancellationToken ct)
    {
        // ParentId == null means top-level category
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.ParentId, (string?)null);
        var sort = Builders<CategoryDocument>.Sort.Ascending(c => c.DisplayOrder);
        var docs = await _col.Find(filter).Sort(sort).ToListAsync(ct);
        return docs.Select(CategoryDocumentMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Category>> GetSubCategoriesAsync(
        CategoryId parentId, CancellationToken ct)
    {
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.ParentId, parentId.Value);
        var docs = await _col.Find(filter).ToListAsync(ct);
        return docs.Select(CategoryDocumentMapper.ToDomain).ToList();
    }

    // Recursively collects all descendant IDs — used for circular reference guard
    public async Task<IReadOnlyList<CategoryId>> GetAllDescendantIdsAsync(
        CategoryId rootId, CancellationToken ct)
    {
        var result = new List<CategoryId>();
        var toVisit = new Queue<string>();
        toVisit.Enqueue(rootId.Value);

        while (toVisit.Count > 0)
        {
            var currentId = toVisit.Dequeue();
            var children = await _col
                .Find(Builders<CategoryDocument>.Filter.Eq(c => c.ParentId, currentId))
                .ToListAsync(ct);

            foreach (var child in children)
            {
                result.Add(CategoryId.Create(child.Id).Value);
                toVisit.Enqueue(child.Id);
            }
        }
        return result;
    }

    public async Task AddAsync(Category category, CancellationToken ct)
    {
        var doc = CategoryDocumentMapper.ToDocument(category);
        doc.Version = 1;
        await _col.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct)
    {
        var doc = CategoryDocumentMapper.ToDocument(category);
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Id, doc.Id);
        var update = Builders<CategoryDocument>.Update
            .Set(c => c.Name, doc.Name)
            .Set(c => c.Slug, doc.Slug)
            .Set(c => c.Description, doc.Description)
            .Set(c => c.ImageUrl, doc.ImageUrl)
            .Set(c => c.ParentId, doc.ParentId)
            .Set(c => c.DisplayOrder, doc.DisplayOrder)
            .Set(c => c.IsActive, doc.IsActive)
            .Set(c => c.UpdatedAt, doc.UpdatedAt)
            .Inc(c => c.Version, 1);
        await _col.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task DeleteAsync(CategoryId id, CancellationToken ct)
    {
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Id, id.Value);
        await _col.DeleteOneAsync(filter, ct);
    }


}
