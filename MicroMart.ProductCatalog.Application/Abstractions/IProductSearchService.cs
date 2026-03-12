using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IProductSearchService
{
    /// Index a new product document (create or update).
    Task IndexProductAsync(
        Product product, CancellationToken ct = default);

    /// Update an existing indexed product document.
    Task UpdateProductIndexAsync(
        Product product, CancellationToken ct = default);

    /// Remove a product from the index (deactivate or delete).
    Task RemoveFromIndexAsync(
        ProductId productId, CancellationToken ct = default);

    /// Full-text search. Returns matching product IDs + total count.
    Task<(IReadOnlyList<string> ProductIds, long TotalCount)> SearchAsync(
        string searchTerm,
        int page,
        int pageSize,
        string? categoryId = null,
        CancellationToken ct = default);

    /// Prefix autocomplete for search bar dropdown. Returns suggestions.
    Task<IReadOnlyList<SearchSuggestion>> AutocompleteAsync(
        string prefix,
        int maxSuggestions = 10,
        CancellationToken ct = default);
}

// Internal DTO returned by AutocompleteAsync
public sealed record SearchSuggestion(
    string Text,
    string? ProductId,
    string? Slug);