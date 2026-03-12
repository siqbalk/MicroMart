using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.IntegrationEvents;

public sealed record ProductCreatedIntegrationEvent(
    string ProductId,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    string CategoryId,
    int InitialStock
)
{
    // EventId + OccurredOn — for idempotency and ordering
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

