using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _db;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        _settings = options.Value;
        var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
        clientSettings.MaxConnectionPoolSize = _settings.MaxConnectionPoolSize;
        clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(_settings.ServerSelectionTimeoutMs);
        clientSettings.ConnectTimeout = TimeSpan.FromMilliseconds(_settings.ConnectTimeoutMs);
        var client = new MongoClient(clientSettings);
        _db = client.GetDatabase(_settings.DatabaseName);
    }

    // Collections ──────────────────────────────────────────────────────────
    public IMongoCollection<ProductDocument> Products =>
        _db.GetCollection<ProductDocument>(_settings.ProductsCollectionName);

    public IMongoCollection<CategoryDocument> Categories =>
        _db.GetCollection<CategoryDocument>(_settings.CategoriesCollectionName);

    public IMongoCollection<OutboxMessage> OutboxMessages =>
        _db.GetCollection<OutboxMessage>(_settings.OutboxCollectionName);

    // Raw client for multi-document transactions ───────────────────────────
    public async Task<IClientSessionHandle> StartSessionAsync(
        CancellationToken ct = default)
        => await _db.Client.StartSessionAsync(cancellationToken: ct);

    // Index creation — idempotent, safe to call multiple times ─────────────
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await EnsureProductIndexesAsync(ct);
        await EnsureCategoryIndexesAsync(ct);
        await EnsureOutboxIndexesAsync(ct);
    }

    private async Task EnsureProductIndexesAsync(CancellationToken ct)
    {
        var b = Builders<ProductDocument>.IndexKeys;
        await Products.Indexes.CreateManyAsync([
            new(b.Ascending(p => p.Sku),
                new CreateIndexOptions { Unique = true,  Name = "idx_sku_unique"          }),
            new(b.Ascending(p => p.Slug),
                new CreateIndexOptions { Unique = true,  Name = "idx_slug_unique"         }),
            new(b.Ascending(p => p.CategoryId),
                new CreateIndexOptions { Name = "idx_category_id"            }),
            new(b.Ascending(p => p.IsActive).Ascending(p => p.IsFeatured),
                new CreateIndexOptions { Name = "idx_active_featured"        }),
            new(b.Ascending(p => p.Price),
                new CreateIndexOptions { Name = "idx_price"                   }),
            new(b.Ascending(p => p.Stock),
                new CreateIndexOptions { Name = "idx_stock"                   }),
            new(b.Ascending(p => p.CategoryId).Ascending(p => p.IsActive).Ascending(p => p.Price),
                new CreateIndexOptions { Name = "idx_category_active_price"  }),
            new(b.Ascending(p => p.Tags),
                new CreateIndexOptions { Name = "idx_tags"                    }),
            new(b.Descending(p => p.UpdatedAt),
                new CreateIndexOptions { Name = "idx_updated_at"             }),
        ], ct);
    }

    private async Task EnsureCategoryIndexesAsync(CancellationToken ct)
    {
        var b = Builders<CategoryDocument>.IndexKeys;
        await Categories.Indexes.CreateManyAsync([
            new(b.Ascending(c => c.Slug),
                new CreateIndexOptions { Unique = true, Name = "idx_category_slug_unique" }),
            new(b.Ascending(c => c.ParentId),
                new CreateIndexOptions { Sparse = true, Name = "idx_category_parent_id"   }),
            new(b.Ascending(c => c.DisplayOrder),
                new CreateIndexOptions { Name = "idx_display_order"            }),
        ], ct);
    }

    private async Task EnsureOutboxIndexesAsync(CancellationToken ct)
    {
        var b = Builders<OutboxMessage>.IndexKeys;
        await OutboxMessages.Indexes.CreateManyAsync([
            new(b.Ascending(o => o.ProcessedAt).Ascending(o => o.CreatedAt),
                new CreateIndexOptions { Sparse = true, Name = "idx_outbox_unprocessed" }),
            new(b.Ascending(o => o.ProcessedAt),
                new CreateIndexOptions {
                    ExpireAfter = TimeSpan.FromDays(7), // auto-delete after 7 days
                    Sparse = true, Name = "idx_outbox_ttl" }),
        ], ct);
    }
}
