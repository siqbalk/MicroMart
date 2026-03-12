using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Events;


public abstract record DomainEvent(Guid EventId, DateTime OccurredOn)
    : IDomainEvent
{
    protected DomainEvent()
        : this(Guid.NewGuid(), DateTime.UtcNow) { }
}

public sealed record ProductCreatedEvent(
    ProductId ProductId,
    string Name,
    string Sku,
    Money Price,
    CategoryId CategoryId,
    int InitialStock
) : DomainEvent;
