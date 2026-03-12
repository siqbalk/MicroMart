using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.DTOs;

public sealed record ProductResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal EffectivePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsOnSale { get; init; }
    public bool IsInStock { get; init; }
    public bool IsLowStock { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public string CategoryId { get; init; } = string.Empty;

    // Flattened dimensions
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }

    public Dictionary<string, string> Attributes { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<ProductImageResponse> Images { get; init; } = [];
    public IReadOnlyList<ProductVariantResponse> Variants { get; init; } = [];

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

