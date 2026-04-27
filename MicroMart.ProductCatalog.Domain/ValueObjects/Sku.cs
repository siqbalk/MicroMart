using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Sku
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Result<Sku> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Sku>(
                Error.Validation("Sku", "SKU is required"));

        var upper = value.ToUpperInvariant().Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(
            upper, @"^[A-Z0-9\-]{2,50}$"))
            return Result.Failure<Sku>(
                Error.Validation("Sku",
                    "SKU must be 2-50 uppercase alphanumeric characters or hyphens"));

        return Result.Success(new Sku(upper));
    }

    public override string ToString() => Value;
}
