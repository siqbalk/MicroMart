using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.IntegrationEvents;

public sealed record ProductPriceChangedIntegrationEvent(
    string ProductId,
    decimal NewPrice,
    string Currency
)
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
