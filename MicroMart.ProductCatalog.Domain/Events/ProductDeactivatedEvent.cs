using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record ProductDeactivatedEvent(
    ProductId ProductId,
    string Name
) : DomainEvent;
