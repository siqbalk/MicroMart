using global::MassTransit;
using global::MicroMart.ProductCatalog.Application.IntegrationEvents;
using global::MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;
using global::MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit;

// Background service that reads pending outbox messages and publishes them
// to RabbitMQ via MassTransit. Runs every 5 seconds.
// Guarantees at-least-once delivery — consumers must be idempotent.
public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,        // ← inject this, NOT IPublishEndpoint
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox processor encountered an error.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("Outbox processor stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        // ── Create a fresh scope for each batch ──────────────────────────
        // This gives us a properly scoped IPublishEndpoint and MongoDbContext
        // and ensures they are disposed when the batch completes.
        await using var scope = scopeFactory.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        // Fetch pending messages — not yet processed, under max retries
        var pending = await context.OutboxMessages
            .Find(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .SortBy(m => m.CreatedAt)
            .Limit(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        logger.LogDebug("Processing {Count} outbox messages.", pending.Count);

        foreach (var message in pending)
        {
            await ProcessMessageAsync(context, publishEndpoint, message, ct);
        }
    }

    private async Task ProcessMessageAsync(
        MongoDbContext context,
        IPublishEndpoint publishEndpoint,
        OutboxMessage message,
        CancellationToken ct)
    {
        try
        {
            // Resolve the CLR type from the stored type name
            var type = Type.GetType(message.Type);
            if (type is null)
            {
                logger.LogWarning(
                    "Cannot resolve type '{Type}' for outbox message {Id}. Skipping.",
                    message.Type, message.Id);

                await MarkFailedAsync(context, message, ct);
                return;
            }

            // Deserialize the JSON payload back to the original event type
            var @event = JsonSerializer.Deserialize(message.Payload, type);
            if (@event is null)
            {
                logger.LogWarning(
                    "Failed to deserialize outbox message {Id}. Skipping.",
                    message.Id);

                await MarkFailedAsync(context, message, ct);
                return;
            }

            // Publish to RabbitMQ via MassTransit
            await publishEndpoint.Publish(@event, type, ct);

            // Mark as processed
            await context.OutboxMessages.UpdateOneAsync(
                m => m.Id == message.Id,
                Builders<OutboxMessage>.Update
                    .Set(m => m.ProcessedAt, DateTime.UtcNow),
                cancellationToken: ct);

            logger.LogDebug("Published outbox message {Id} of type {Type}.", message.Id, message.Type);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to publish outbox message {Id}. RetryCount = {RetryCount}.",
                message.Id, message.RetryCount + 1);

            await MarkFailedAsync(context, message, ct);
        }
    }

    private static async Task MarkFailedAsync(
        MongoDbContext context,
        OutboxMessage message,
        CancellationToken ct)
    {
        await context.OutboxMessages.UpdateOneAsync(
            m => m.Id == message.Id,
            Builders<OutboxMessage>.Update
                .Inc(m => m.RetryCount, 1),
            cancellationToken: ct);
    }
}