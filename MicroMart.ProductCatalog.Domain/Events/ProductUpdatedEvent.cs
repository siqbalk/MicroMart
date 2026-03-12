using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Events;

public sealed record ProductUpdatedEvent(
    ProductId ProductId,
    string NewName
) : DomainEvent;

