using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.DTOs;

public sealed record ProductVariantResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public bool IsActive { get; init; }
    public Dictionary<string, string> Options { get; init; } = [];
}