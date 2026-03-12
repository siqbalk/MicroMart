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

public sealed class ProductRepository(MongoDbContext context) : IProductRepository
{
    private readonly IMongoCollection<ProductDocument> _col = context.Products;

    // ── Single-record reads ──────────────────────────────────────────────

    public async Task<Product?> FindByIdAsync(ProductId id, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Id, id.Value);
        var doc = await _col.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<Product?> FindBySlugAsync(string slug, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Slug, slug);
        var doc = await _col.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    public async Task<Product?> FindBySkuAsync(string sku, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Sku, sku);
        var doc = await _col.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : ProductDocumentMapper.ToDomain(doc);
    }

    // ── Existence checks (for validators) ────────────────────────────────

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Sku, sku);
        return await _col.Find(filter).AnyAsync(ct);
    }

    // ── Batch fetch (for search results: ES returns IDs → fetch from Mongo) ─

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids, CancellationToken ct)
    {
        var idStrings = ids.Select(i => i.Value).ToList();
        var filter = Builders<ProductDocument>.Filter.In(p => p.Id, idStrings);
        var docs = await _col.Find(filter).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    // ── Paged listing (admin + storefront) ───────────────────────────────

    public async Task<(IReadOnlyList<Product> Items, long TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? categoryId,
        bool? isActive,
        bool? isFeatured,
        string? sortBy,
        bool ascending,
        CancellationToken ct)
    {
        var filterBuilder = Builders<ProductDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (categoryId is not null)
            filter &= filterBuilder.Eq(p => p.CategoryId, categoryId);
        if (isActive.HasValue)
            filter &= filterBuilder.Eq(p => p.IsActive, isActive.Value);
        if (isFeatured.HasValue)
            filter &= filterBuilder.Eq(p => p.IsFeatured, isFeatured.Value);

        // Build sort definition
        SortDefinition<ProductDocument> sort = sortBy?.ToLowerInvariant() switch
        {
            "price" => ascending
                ? Builders<ProductDocument>.Sort.Ascending(p => p.Price)
                : Builders<ProductDocument>.Sort.Descending(p => p.Price),
            "name" => ascending
                ? Builders<ProductDocument>.Sort.Ascending(p => p.Name)
                : Builders<ProductDocument>.Sort.Descending(p => p.Name),
            "rating" => ascending
                ? Builders<ProductDocument>.Sort.Ascending(p => p.AverageRating)
                : Builders<ProductDocument>.Sort.Descending(p => p.AverageRating),
            "stock" => ascending
                ? Builders<ProductDocument>.Sort.Ascending(p => p.Stock)
                : Builders<ProductDocument>.Sort.Descending(p => p.Stock),
            _ => Builders<ProductDocument>.Sort.Descending(p => p.UpdatedAt)
        };

        var skip = (page - 1) * pageSize;

        // Run count and data fetch in parallel
        var countTask = _col.CountDocumentsAsync(filter, cancellationToken: ct);
        var docsTask = _col.Find(filter).Sort(sort).Skip(skip).Limit(pageSize).ToListAsync(ct);

        await Task.WhenAll(countTask, docsTask);

        var items = docsTask.Result.Select(ProductDocumentMapper.ToDomain).ToList();
        return (items, countTask.Result);
    }

    // ── Specific read queries ────────────────────────────────────────────

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(
        int count, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter
            .Where(p => p.IsActive && p.IsFeatured);
        var sort = Builders<ProductDocument>.Sort.Descending(p => p.UpdatedAt);
        var docs = await _col.Find(filter).Sort(sort).Limit(count).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        CategoryId categoryId, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter
            .Where(p => p.CategoryId == categoryId.Value && p.IsActive);
        var docs = await _col.Find(filter).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(
        int threshold, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter
            .Where(p => p.Stock <= threshold && p.IsActive);
        var sort = Builders<ProductDocument>.Sort.Ascending(p => p.Stock);
        var docs = await _col.Find(filter).Sort(sort).ToListAsync(ct);
        return docs.Select(ProductDocumentMapper.ToDomain).ToList();
    }

    public async Task<long> CountByCategoryAsync(
        CategoryId categoryId, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter
            .Eq(p => p.CategoryId, categoryId.Value);
        return await _col.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    // ── Writes ──────────────────────────────────────────────────────────

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        var doc = ProductDocumentMapper.ToDocument(product);
        doc.Version = 1;
        await _col.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct)
    {
        var doc = ProductDocumentMapper.ToDocument(product);

        // Optimistic concurrency: only update if version matches
        var filter = Builders<ProductDocument>.Filter
            .Eq(p => p.Id, doc.Id);

        var update = Builders<ProductDocument>.Update
            .Set(p => p.Name, doc.Name)
            .Set(p => p.Description, doc.Description)
            .Set(p => p.Slug, doc.Slug)
            .Set(p => p.Price, doc.Price)
            .Set(p => p.SalePrice, doc.SalePrice)
            .Set(p => p.Stock, doc.Stock)
            .Set(p => p.IsActive, doc.IsActive)
            .Set(p => p.IsFeatured, doc.IsFeatured)
            .Set(p => p.CategoryId, doc.CategoryId)
            .Set(p => p.Dimensions, doc.Dimensions)
            .Set(p => p.Attributes, doc.Attributes)
            .Set(p => p.Tags, doc.Tags)
            .Set(p => p.Images, doc.Images)
            .Set(p => p.Variants, doc.Variants)
            .Set(p => p.AverageRating, doc.AverageRating)
            .Set(p => p.ReviewCount, doc.ReviewCount)
            .Set(p => p.UpdatedAt, doc.UpdatedAt)
            .Inc(p => p.Version, 1);   // atomically increment version

        await _col.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task DeleteAsync(ProductId id, CancellationToken ct)
    {
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Id, id.Value);
        await _col.DeleteOneAsync(filter, ct);
    }


}