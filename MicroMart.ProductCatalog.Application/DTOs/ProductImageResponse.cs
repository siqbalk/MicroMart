using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.DTOs;

public sealed record ProductImageResponse
{
    public string Url { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
