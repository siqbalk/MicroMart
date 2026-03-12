using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.IntegrationEvents;

public sealed record StockUpdatedIntegrationEvent(
    string ProductId,
    int NewStockLevel,
    int QuantityChanged,
    string ChangeType       // "ADD" or "DEDUCT"
)
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public bool IsLowStock => NewStockLevel > 0 && NewStockLevel <= 10;
    public bool IsOutOfStock => NewStockLevel == 0;
}
