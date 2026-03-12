using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

public sealed class CategoryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("imageUrl")]
    [BsonIgnoreIfNull]
    public string? ImageUrl { get; set; }
    [BsonElement("parentId")]
    [BsonIgnoreIfNull]
    public string? ParentId { get; set; }
    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }
    [BsonElement("version")]
    public int Version { get; set; }
}
