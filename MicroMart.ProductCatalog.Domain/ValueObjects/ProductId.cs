using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record ProductId
{
    public string Value { get; }

    private ProductId(string value) => Value = value;

    // Creates a new unique ProductId
    public static ProductId New()
        => new(Guid.NewGuid().ToString("N"));

    // Creates ProductId from an existing string (from DB, API etc.)
    public static Result<ProductId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<ProductId>(
                Error.Validation("ProductId", "ProductId cannot be empty"));
        return Result.Success(new ProductId(value));
    }

    public override string ToString() => Value;
}
