using MicroMart.Shared.Core.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MicroMart.ProductCatalog.Domain.ValueObjects;

public sealed record Slug
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    // Auto-generate slug from product name
    public static Result<Slug> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Slug>(
                Error.Validation("Slug", "Name is required to generate slug"));

        // 1. lowercase
        var slug = name.ToLowerInvariant().Trim();

        // 2. replace common chars
        slug = slug
            .Replace("&", "and")
            .Replace("@", "at")
            .Replace("+", "plus")
            .Replace(" ", "-")
            .Replace("_", "-");

        // 3. remove non-alphanumeric (except hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // 4. collapse multiple hyphens
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        if (string.IsNullOrEmpty(slug))
            return Result.Failure<Slug>(
                Error.Validation("Slug", "Slug cannot be empty after normalisation"));

        if (slug.Length > 250)
            slug = slug[..250].TrimEnd('-');

        return Result.Success(new Slug(slug));
    }

    public static Slug CreateFromExisting(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Cannot reconstitute a Slug from an empty value. Data is corrupted.");

        return new Slug(value);
    }


    // Append suffix for duplicate slugs: "iphone-15" → "iphone-15-2"
    public Slug WithSuffix(int suffix)
        => new($"{Value}-{suffix}");

    public override string ToString() => Value;
}
