using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit.Consumers;

public sealed class ProductDeletedConsumer(ICacheService cache,ILogger<ProductDeletedConsumer> logger)
    : IConsumer<ProductDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductDeletedIntegrationEvent> ctx)
    {
        var @event = ctx.Message;
        logger.LogInformation(
            "Consuming ProductDeleted for {Id}", @event.ProductId);

        await Task.WhenAll(
            cache.RemoveAsync($"product:id:{@event.ProductId}", ctx.CancellationToken),
            cache.RemoveByPatternAsync("products:paged:*", ctx.CancellationToken),
            cache.RemoveByPatternAsync("products:featured:*", ctx.CancellationToken)
        );
    }
}
