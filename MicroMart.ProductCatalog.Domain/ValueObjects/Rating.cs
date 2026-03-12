

using MicroMart.ProductCatalog.Domain.Primitives;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Rating
{
    public int Value { get; }

    private Rating(int value) => Value = value;

    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
            return Result.Failure<Rating>(
                Error.Validation("Rating", "Rating must be between 1 and 5"));

        return Result.Success(new Rating(value));
    }

    public bool IsPositive => Value >= 4;
    public bool IsNegative => Value <= 2;
}
