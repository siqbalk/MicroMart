using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

public sealed class ProductVariantDocument
{
    [BsonRepresentation(BsonType.String)]
    [BsonElement("id")]
    public Guid Id { get; set; }   // ← must store the entity Id

    [BsonElement("name")] public string Name { get; set; } = string.Empty;
    [BsonElement("sku")] public string Sku { get; set; } = string.Empty;
    [BsonElement("price")] public MoneyDocument Price { get; set; } = new();
    [BsonElement("stock")] public int Stock { get; set; }
    [BsonElement("isActive")] public bool IsActive { get; set; }
    [BsonElement("options")] public Dictionary<string, string> Options { get; set; } = [];
}
