using MicroMart.Shared.Core.Primitives;

namespace MicroMart.ProductCatalog.Domain.Entities;

public sealed class ProductImage : Entity<Guid>
{
    public string Url { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime AddedAt { get; private set; }

    private ProductImage() { }

    internal static ProductImage Create(
        string url, string altText, bool isPrimary)
        => new()
        {
            Id = Guid.NewGuid(),
            Url = url,
            AltText = altText,
            IsPrimary = isPrimary,
            AddedAt = DateTime.UtcNow
        };

    public static ProductImage Reconstitute(
       Guid id,
       string url,
       string altText,
       bool isPrimary,
       int sortOrder,
       DateTime addedAt)
       => new()
       {
           Id = id,
           Url = url,
           AltText = altText,
           IsPrimary = isPrimary,
           SortOrder = sortOrder,
           AddedAt = addedAt
       };

    internal void SetAsPrimary(bool isPrimary)
        => IsPrimary = isPrimary;
}

