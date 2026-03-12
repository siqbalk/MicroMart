using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Settings;

public sealed class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ProductsIndexName { get; set; } = "products";
    public int NumberOfShards { get; set; } = 1;
    public int NumberOfReplicas { get; set; } = 1;
}
