using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.Shared.Core.Primitives;

    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }

