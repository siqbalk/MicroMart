using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MicroMart.ProductCatalog.Infrastructure.Search.Elasticsearch;

public sealed class ProductSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isInStock")]
    public bool IsInStock { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = [];

    [JsonPropertyName("suggest")]
    public string Suggest { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public decimal Rating { get; set; }

    [JsonPropertyName("reviewCount")]
    public int ReviewCount { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
