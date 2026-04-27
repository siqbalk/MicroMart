using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string? currency)
    {
        if (amount < 0)
            return Result.Failure<Money>(
                Error.Validation("Money", "Amount cannot be negative"));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Money>(
                Error.Validation("Money", "Currency must be a 3-letter ISO code"));

        return Result.Success(
            new Money(Math.Round(amount, 2), currency.ToUpperInvariant()));
    }

    // Convenience factories
    public static Money Usd(decimal amount) => new(amount, "USD");
    public static Money Aed(decimal amount) => new(amount, "AED");
    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot add {Currency} and {other.Currency}");
        return new(Math.Round(Amount + other.Amount, 2), Currency);
    }

    public Money Multiply(int factor)
        => new(Math.Round(Amount * factor, 2), Currency);

    public Money ApplyDiscount(decimal percentOff)
    {
        if (percentOff < 0 || percentOff > 100)
            throw new ArgumentException("Discount must be 0-100");
        return new(Math.Round(Amount * (1 - percentOff / 100), 2), Currency);
    }

    public bool IsGreaterThan(Money other) => Amount > other.Amount;
    public override string ToString() => $"{Amount:F2} {Currency}";
}
