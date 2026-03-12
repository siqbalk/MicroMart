using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record ProductPriceChangedEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice
) : DomainEvent
{
    // Computed for convenience — was it an increase or decrease?
    public bool WasPriceReduced => NewPrice.Amount < OldPrice.Amount;
    public decimal ChangePercent =>
        OldPrice.Amount == 0 ? 0
        : Math.Round((NewPrice.Amount - OldPrice.Amount) / OldPrice.Amount * 100, 2);
}
