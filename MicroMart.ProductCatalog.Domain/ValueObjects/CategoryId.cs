using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record CategoryId
{
    public string Value { get; }

    private CategoryId(string value) => Value = value;

    public static CategoryId New()
        => new(Guid.NewGuid().ToString("N"));

    public static Result<CategoryId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<CategoryId>(
                Error.Validation("CategoryId", "CategoryId cannot be empty"));
        return Result.Success(new CategoryId(value));
    }

    public override string ToString() => Value;
}

