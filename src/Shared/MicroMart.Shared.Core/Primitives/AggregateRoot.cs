using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.Shared.Core.Primitives;

    public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        protected AggregateRoot(TId id) : base(id) { }
        protected AggregateRoot() { }

        // Read-only view — nobody outside can add events directly
        public IReadOnlyList<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        // Aggregate methods call this to raise events
        protected void Raise(IDomainEvent domainEvent)
            => _domainEvents.Add(domainEvent);

        // Infrastructure calls this AFTER SaveChanges to dispatch events
        public void ClearDomainEvents()
            => _domainEvents.Clear();
    }

