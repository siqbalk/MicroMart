using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.DTOs;

public sealed record ProductSummaryResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal EffectivePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? PrimaryImage { get; init; } // URL of primary image only
    public bool IsOnSale { get; init; }
    public bool IsInStock { get; init; }
    public bool IsLowStock { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public string CategoryId { get; init; } = string.Empty;
}
