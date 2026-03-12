using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record StockUpdatedEvent(
    ProductId ProductId,
    int NewStockLevel,
    int QuantityChanged,
    string ChangeType          // "ADD" or "DEDUCT"
) : DomainEvent
{
    public bool IsLowStock => NewStockLevel > 0 && NewStockLevel <= 10;
    public bool IsOutOfStock => NewStockLevel == 0;
}