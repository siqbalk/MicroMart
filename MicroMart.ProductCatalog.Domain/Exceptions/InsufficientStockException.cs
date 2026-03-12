using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public ProductId ProductId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(
        ProductId productId, int requested, int available)
        : base(
            $"Insufficient stock for product '{productId}'. " +
            $"Requested: {requested}, Available: {available}")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }
}
