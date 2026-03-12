using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

public sealed class ProductImageDocument
{
    [BsonElement("url")] public string Url { get; set; } = string.Empty;
    [BsonElement("altText")] public string AltText { get; set; } = string.Empty;
    [BsonElement("isPrimary")] public bool IsPrimary { get; set; }
    [BsonElement("sortOrder")] public int SortOrder { get; set; }
    [BsonElement("addedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AddedAt { get; set; }   // ← add this
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }   // ← add this too
}
