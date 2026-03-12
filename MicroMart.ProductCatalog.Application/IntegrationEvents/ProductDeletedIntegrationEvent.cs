using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.IntegrationEvents;

public sealed record ProductDeletedIntegrationEvent(
    string ProductId,
    string Name
)
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
