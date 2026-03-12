

using global::MassTransit;
using global::MicroMart.ProductCatalog.Application.IntegrationEvents;
using global::MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;
using global::MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit;

// Background service that reads pending outbox messages and publishes them
// to RabbitMQ via MassTransit. Runs every 5 seconds.
// Guarantees at-least-once delivery — consumers must be idempotent.
public sealed class OutboxProcessorService(
    MongoDbContext context,
    IPublishEndpoint bus,
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private const int _batchSize = 50;
    private const int _maxRetries = 5;

    // Well-known integration event types for deserialization
    private static readonly Dictionary<string, Type> _eventTypes = new()
    {
        [nameof(ProductCreatedIntegrationEvent)] = typeof(ProductCreatedIntegrationEvent),
        [nameof(ProductDeletedIntegrationEvent)] = typeof(ProductDeletedIntegrationEvent),
        [nameof(ProductPriceChangedIntegrationEvent)] = typeof(ProductPriceChangedIntegrationEvent),
        [nameof(StockUpdatedIntegrationEvent)] = typeof(StockUpdatedIntegrationEvent),
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started — polling every {Interval}s",
            _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox processor encountered an error");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        // Fetch pending messages (processedAt == null) ordered by creation time
        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(m => m.ProcessedAt, (DateTime?)null),
            Builders<OutboxMessage>.Filter.Lt(m => m.RetryCount, _maxRetries));

        var sort = Builders<OutboxMessage>.Sort.Ascending(m => m.CreatedAt);

        var messages = await context.OutboxMessages
            .Find(filter).Sort(sort).Limit(_batchSize).ToListAsync(ct);

        if (messages.Count == 0) return;

        logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await PublishMessageAsync(message, ct);
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        try
        {
            // Resolve type from simple name (more portable than AssemblyQualifiedName)
            var typeName = message.Type.Split('.').Last().Split(',').First();
            if (!_eventTypes.TryGetValue(typeName, out var eventType))
            {
                logger.LogWarning(
                    "Unknown outbox message type '{Type}' — marking as processed to prevent loop",
                    message.Type);
                await MarkProcessedAsync(message.Id, ct);
                return;
            }

            // Deserialize the JSON payload back to the integration event
            var @event = JsonSerializer.Deserialize(message.Payload, eventType);
            if (@event == null)
            {
                logger.LogError(
                    "Failed to deserialize outbox message {Id} of type '{Type}'",
                    message.Id, message.Type);
                await MarkFailedAsync(message.Id, "Deserialization returned null", ct);
                return;
            }

            // Publish to RabbitMQ via MassTransit
            // MassTransit routes to the correct exchange by message type
            await bus.Publish(@event, eventType, ct);
            await MarkProcessedAsync(message.Id, ct);

            logger.LogDebug(
                "Published outbox message {Id} of type '{Type}'",
                message.Id, typeName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish outbox message {Id} — incrementing retry count",
                message.Id);
            await MarkFailedAsync(message.Id, ex.Message, ct);
        }
    }

    private async Task MarkProcessedAsync(string id, CancellationToken ct)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, id);
        var update = Builders<OutboxMessage>.Update
            .Set(m => m.ProcessedAt, DateTime.UtcNow)
            .Unset(m => m.Error);
        await context.OutboxMessages.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    private async Task MarkFailedAsync(string id, string error, CancellationToken ct)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(m => m.Id, id);
        var update = Builders<OutboxMessage>.Update
            .Set(m => m.Error, error)
            .Inc(m => m.RetryCount, 1);
        await context.OutboxMessages.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}