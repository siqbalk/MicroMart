using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.IntegrationEvents;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit.Consumers;

public sealed class StockUpdatedConsumer(
    Application.Abstractions.IProductSearchService search,
    IProductRepository productRepo,
    ICacheService cache,
    ILogger<StockUpdatedConsumer> logger)
    : IConsumer<StockUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<StockUpdatedIntegrationEvent> ctx)
    {
        var @event = ctx.Message;
        logger.LogInformation(
            "Consuming StockUpdated for product {Id} — new stock: {Stock}",
            @event.ProductId, @event.NewStockLevel);

        // Re-index product so Elasticsearch IsInStock field is current
        var idResult = ProductId.Create(@event.ProductId);
        if (idResult.IsSuccess)
        {
            var product = await productRepo.FindByIdAsync(idResult.Value, ctx.CancellationToken);
            if (product is not null)
                await search.UpdateProductIndexAsync(product, ctx.CancellationToken);
        }

        // Evict cached stock-sensitive keys
        await cache.RemoveAsync($"product:id:{@event.ProductId}", ctx.CancellationToken);
        await cache.RemoveByPatternAsync("products:lowstock:*", ctx.CancellationToken);
    }
}
