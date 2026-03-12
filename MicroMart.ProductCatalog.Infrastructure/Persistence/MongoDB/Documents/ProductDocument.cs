using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;


public sealed class ProductDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;
    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;
    [BsonElement("price")]
    public MoneyDocument Price { get; set; } = new();
    [BsonElement("salePrice")]
    [BsonIgnoreIfNull]
    public MoneyDocument? SalePrice { get; set; }
    [BsonElement("stock")]
    public int Stock { get; set; }
    [BsonElement("isActive")]
    public bool IsActive { get; set; }
    [BsonElement("isFeatured")]
    public bool IsFeatured { get; set; }
    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;
    [BsonElement("dimensions")]
    public DimensionsDocument Dimensions { get; set; } = new();
    [BsonElement("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = [];
    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];
    [BsonElement("images")]
    public List<ProductImageDocument> Images { get; set; } = [];
    [BsonElement("variants")]
    public List<ProductVariantDocument> Variants { get; set; } = [];
    [BsonElement("averageRating")]
    public decimal AverageRating { get; set; }
    [BsonElement("reviewCount")]
    public int ReviewCount { get; set; }
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("version")]
    public int Version { get; set; }  // optimistic concurrency
}

public sealed class MoneyDocument
{
    [BsonElement("amount")] public decimal Amount { get; set; }
    [BsonElement("currency")] public string Currency { get; set; } = "USD";
}

public sealed class DimensionsDocument
{
    [BsonElement("weightKg")] public decimal WeightKg { get; set; }
    [BsonElement("lengthCm")] public decimal LengthCm { get; set; }
    [BsonElement("widthCm")] public decimal WidthCm { get; set; }
    [BsonElement("heightCm")] public decimal HeightCm { get; set; }
}
