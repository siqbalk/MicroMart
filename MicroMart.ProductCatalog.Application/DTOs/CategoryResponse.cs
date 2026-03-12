using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.DTOs;

public sealed record CategoryResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? ParentId { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsTopLevel { get; init; }
}