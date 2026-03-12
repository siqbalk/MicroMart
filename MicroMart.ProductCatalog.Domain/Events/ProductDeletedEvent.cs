using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record ProductDeletedEvent(
    ProductId ProductId,
    string Name
) : DomainEvent;
