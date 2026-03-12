using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Primitives;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
  where TId : notnull
{
    protected Entity(TId id) => Id = id;
    protected Entity() { } // required for MongoDB deserialization

    public TId Id { get; protected set; } = default!;

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left is not null && right is not null && left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (other.GetType() != GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
        => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode()
        => Id.GetHashCode() * 31;
}

