using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace MicroMart.ProductCatalog.Infrastructure.Search.Elasticsearch;

public sealed class ElasticsearchProductSearchService(
    ElasticsearchClient client,
    IOptions<ElasticsearchSettings> options,
    ILogger<ElasticsearchProductSearchService> logger)
    : IProductSearchService
{
    private readonly string _index = options.Value.ProductsIndexName;

    // ── Writes ───────────────────────────────────────────────────────────

    public async Task IndexProductAsync(Product product, CancellationToken ct)
    {
        var doc = ToSearchDocument(product);
        var response = await client.IndexAsync(doc, i => i
            .Index(_index)
            .Id(doc.Id), ct);

        if (!response.IsValidResponse)
            logger.LogError(
                "Failed to index product {Id}: {Error}",
                product.Id.Value, response.ElasticsearchServerError?.Error?.Reason);
    }

    public async Task UpdateProductIndexAsync(Product product, CancellationToken ct)
    {
        // Upsert — creates or replaces the document
        await IndexProductAsync(product, ct);
    }

    public async Task RemoveFromIndexAsync(ProductId productId, CancellationToken ct)
    {
        var response = await client.DeleteAsync(
            new DeleteRequest(_index, productId.Value), ct);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
            logger.LogError(
                "Failed to delete product {Id} from Elasticsearch", productId.Value);
    }

    // ── Full-text Search ─────────────────────────────────────────────────

    public async Task<(IReadOnlyList<string> ProductIds, long TotalCount)> SearchAsync(
     string searchTerm,
     int page,
     int pageSize,
     string? categoryId,
     CancellationToken ct)
    {
        var from = (page - 1) * pageSize;

        var response = await client.SearchAsync<ProductSearchDocument>(s => s
            .Index(_index)
            .From(from)
            .Size(pageSize)
            .Query(q => q
                .Bool(b =>
                {
                    // ── Filter: only active + in-stock ───────────────────────
                    var filters = new List<Action<QueryDescriptor<ProductSearchDocument>>>
                    {
                    f => f.Term(t => t.Field(p => p.IsActive).Value(true)),
                    f => f.Term(t => t.Field(p => p.IsInStock).Value(true))
                    };

                    // Optional category filter
                    if (categoryId is not null)
                        filters.Add(f => f.Term(t => t
                            .Field(p => p.CategoryId).Value(categoryId)));

                    b.Filter(filters.ToArray());

                    // ── Should: relevance scoring ─────────────────────────────
                    b.Should(s => s
                        .MultiMatch(m => m
                            .Fields(new[]
                            {
                            "name^5",
                            "description^2",
                            "tags^3",
                            "sku^4",
                            "attributes.*"
                            })
                            .Query(searchTerm)
                            .Type(TextQueryType.BestFields)
                            .Fuzziness(new Fuzziness("AUTO"))
                            .MinimumShouldMatch("70%")));

                    b.MinimumShouldMatch(1);
                }))
            .Sort(so => so
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field(f => f
                    .Field(p => p.Rating)
                    .Order(SortOrder.Desc))), ct);

        if (!response.IsValidResponse)
        {
            logger.LogError("Elasticsearch search failed: {Error}",
                response.ElasticsearchServerError?.Error?.Reason);
            return ([], 0);
        }

        var ids = response.Hits.Select(h => h.Id!).ToList();
        var total = response.Total;
        return (ids, total);
    }

    // ── Autocomplete ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SearchSuggestion>> AutocompleteAsync(
    string prefix,
    int maxSuggestions,
    CancellationToken ct)
    {
        const string SuggestName = "product-suggest";

        var response = await client.SearchAsync<ProductSearchDocument>(s => s
            .Index(_index)
            .Size(0)
            .Suggest(su => su
                .AddSuggester(SuggestName, sg => sg
                    .Prefix(prefix) // ✅ prefix goes here
                    .Completion(c => c
                        .Field(p => p.Suggest)
                        .Size(maxSuggestions)
                        .SkipDuplicates(true)
                    )
                )
            ), ct);

        if (!response.IsValidResponse)
            return [];

        var options = response.Suggest?
            .GetCompletion(SuggestName)?
            .FirstOrDefault()?
            .Options ?? [];

        return options
            .Select(o => new SearchSuggestion(
                Text: o.Text,
                ProductId: o.Source?.Id,
                Slug: o.Source?.Slug))
            .ToList();
    }

    // ── Domain → Search Document ─────────────────────────────────────────

    private static ProductSearchDocument ToSearchDocument(Product p) => new()
    {
        Id = p.Id.Value,
        Name = p.Name,
        Description = p.Description,
        Slug = p.Slug.Value,
        Sku = p.Sku.Value,
        CategoryId = p.CategoryId.Value,
        Price = p.EffectivePrice.Amount,
        IsActive = p.IsActive,
        IsInStock = p.IsInStock,
        Tags = p.Tags.Select(t => t.Value).ToList(),
        Attributes = p.Attributes.ToDictionary(k => k.Key, v => v.Value),
        Suggest = p.Name,  // drives autocomplete completion field
        Rating = p.AverageRating,
        ReviewCount = p.ReviewCount,
        UpdatedAt = p.UpdatedAt
    };
}