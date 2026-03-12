using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Primitives;

    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }

