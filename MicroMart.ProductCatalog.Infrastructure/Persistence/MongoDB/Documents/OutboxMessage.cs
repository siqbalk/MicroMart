using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

public sealed class OutboxMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Full qualified type name of the integration event
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    // JSON-serialized integration event payload
    [BsonElement("payload")]
    public string Payload { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // null = pending. Set when successfully published to RabbitMQ.
    [BsonElement("processedAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAt { get; set; }

    [BsonElement("error")]
    [BsonIgnoreIfNull]
    public string? Error { get; set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; set; }
}
