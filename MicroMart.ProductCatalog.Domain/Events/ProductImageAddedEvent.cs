using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record ProductImageAddedEvent(
    ProductId ProductId,
    string ImageUrl,
    bool IsPrimary
) : DomainEvent;
