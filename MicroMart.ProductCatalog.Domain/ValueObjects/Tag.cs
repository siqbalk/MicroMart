using MicroMart.ProductCatalog.Domain.Primitives;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Tag
{
    public string Value { get; }
    private Tag(string value) => Value = value;

    public static Result<Tag> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Tag>(
                Error.Validation("Tag", "Tag cannot be empty"));

        var normalised = value.ToLowerInvariant().Trim();

        if (normalised.Length > 50)
            return Result.Failure<Tag>(
                Error.Validation("Tag", "Tag cannot exceed 50 characters"));

        return Result.Success(new Tag(normalised));
    }

    public override string ToString() => Value;
}
