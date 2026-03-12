using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace MicroMart.ProductCatalog.Infrastructure.Search.Elasticsearch;

// Runs once at startup. Creates the Elasticsearch products index with
// correct field mappings, analyzers, and the completion suggester.
// Idempotent — skips creation if index already exists.
public sealed class ElasticsearchIndexInitializer(
    ElasticsearchClient client,
    IOptions<ElasticsearchSettings> options,
    ILogger<ElasticsearchIndexInitializer> logger)
    : IHostedService
{
    private readonly ElasticsearchSettings _settings = options.Value;

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var exists = await client.Indices.ExistsAsync(_settings.ProductsIndexName, ct);
            if (exists.Exists)
            {
                logger.LogInformation(
                    "Elasticsearch index '{Index}' already exists — skipping",
                    _settings.ProductsIndexName);
                return;
            }

            // Build properties using object initializers — avoids ALL fluent naming conflicts
            var properties = new Properties
        {
            { "id",          new KeywordProperty() },
            { "name",        new TextProperty    { Analyzer = "product_analyzer" } },
            { "description", new TextProperty    { Analyzer = "product_analyzer" } },
            { "slug",        new KeywordProperty() },
            { "sku",         new KeywordProperty() },
            { "categoryId",  new KeywordProperty() },
            { "price",       new FloatNumberProperty() },
            { "isActive",    new BooleanProperty() },
            { "isInStock",   new BooleanProperty() },
            { "tags",        new TextProperty    { Analyzer = "product_analyzer" } },
            { "attributes",  new ObjectProperty  { Dynamic = DynamicMapping.True } },
            { "suggest",     new CompletionProperty
                {
                    Analyzer                  = "autocomplete_analyzer",
                    SearchAnalyzer            = "standard",
                    PreserveSeparators        = true,
                    PreservePositionIncrements = true,
                    MaxInputLength            = 50
                }
            },
            { "rating",      new FloatNumberProperty() },
            { "reviewCount", new IntegerNumberProperty() },
            { "updatedAt",   new DateProperty() }
        };

            var response = await client.Indices.CreateAsync(_settings.ProductsIndexName, c => c
                .Settings(s => s
                    .NumberOfShards(_settings.NumberOfShards)
                    .NumberOfReplicas(_settings.NumberOfReplicas)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("product_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filter(["lowercase", "asciifolding", "stop"]))
                            .Custom("autocomplete_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filter(["lowercase", "asciifolding"])))))
                .Mappings(m => m
                    .Properties(properties)), ct);

            if (response.IsValidResponse)
                logger.LogInformation(
                    "Elasticsearch index '{Index}' created successfully",
                    _settings.ProductsIndexName);
            else
                logger.LogError(
                    "Failed to create Elasticsearch index '{Index}': {Error}",
                    _settings.ProductsIndexName,
                    response.ElasticsearchServerError?.Error?.Reason);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Elasticsearch index initialization failed.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}