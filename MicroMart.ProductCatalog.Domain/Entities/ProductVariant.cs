using MicroMart.ProductCatalog.Domain.Primitives;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Entities;

public sealed class ProductVariant : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public Money Price { get; private set; } = default!;
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    public Dictionary<string, string> Options { get; private set; } = [];
    // Options: { "Size": "XL", "Color": "Red" }

    private ProductVariant() { }

    internal static Result<ProductVariant> Create(
        string name, string sku, Money price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ProductVariant>(
                Error.Validation("ProductVariant", "Variant name is required"));

        return Result.Success(new ProductVariant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Sku = sku.ToUpperInvariant(),
            Price = price,
            Stock = stock,
            IsActive = true
        });
    }

    public static ProductVariant Reconstitute(
       Guid id,
       string name,
       string sku,
       Money price,
       int stock,
       bool isActive,
       Dictionary<string, string> options)
       => new()
       {
           Id = id,
           Name = name,
           Sku = sku,
           Price = price,
           Stock = stock,
           IsActive = isActive,
           Options = options
       };

    public Result DeductStock(int qty)
    {
        if (qty > Stock)
            return Result.Failure(
                Error.Validation("ProductVariant",
                    $"Insufficient variant stock: {Stock} available"));
        Stock -= qty;
        return Result.Success();
    }
}
