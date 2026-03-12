using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Mappers;

public static class ProductDocumentMapper
{
    // Domain → Document  (writes: Add, Update)
    public static ProductDocument ToDocument(Product p) => new()
    {
        Id = p.Id.Value,
        Name = p.Name,
        Description = p.Description,
        Slug = p.Slug.Value,
        Sku = p.Sku.Value,
        CategoryId = p.CategoryId.Value,
        Stock = p.Stock,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        AverageRating = p.AverageRating,
        ReviewCount = p.ReviewCount,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,

        Price = new MoneyDocument { Amount = p.Price.Amount, Currency = p.Price.Currency },
        SalePrice = p.SalePrice == null ? null
            : new MoneyDocument { Amount = p.SalePrice.Amount, Currency = p.SalePrice.Currency },

        Dimensions = new DimensionsDocument
        {
            WeightKg = p.Dimensions.WeightKg,
            LengthCm = p.Dimensions.LengthCm,
            WidthCm = p.Dimensions.WidthCm,
            HeightCm = p.Dimensions.HeightCm
        },

        Attributes = p.Attributes.ToDictionary(k => k.Key, v => v.Value),
        Tags = p.Tags.Select(t => t.Value).ToList(),

        Images = p.Images.Select(i => new ProductImageDocument
        {
            Url = i.Url,
            AltText = i.AltText,
            IsPrimary = i.IsPrimary,
            SortOrder = i.SortOrder
        }).ToList(),

        Variants = p.Variants.Select(v => new ProductVariantDocument
        {
            Id = v.Id,
            Name = v.Name,
            Sku = v.Sku,
            Stock = v.Stock,
            IsActive = v.IsActive,
            Price = new MoneyDocument { Amount = v.Price.Amount, Currency = v.Price.Currency },
            Options = v.Options.ToDictionary(o => o.Key, o => o.Value)
        }).ToList()
    };

    // Document → Domain  (reads: FindById, GetPaged, etc.)
    public static Product ToDomain(ProductDocument d)
    {
        var id = ProductId.Create(d.Id).Value;
        var sku = Sku.Create(d.Sku).Value;
        var slug = Slug.CreateFromExisting(d.Slug);   // bypasses slug generation
        var categoryId = CategoryId.Create(d.CategoryId).Value;
        var price = Money.Create(d.Price.Amount, d.Price.Currency).Value;
        var salePrice = d.SalePrice == null ? (Money?)null
            : Money.Create(d.SalePrice.Amount, d.SalePrice.Currency).Value;
        var dimensions = Dimensions.Create(
            d.Dimensions.WeightKg, d.Dimensions.LengthCm,
            d.Dimensions.WidthCm, d.Dimensions.HeightCm).Value;

        var tags = d.Tags
            .Select(Tag.Create).Where(r => r.IsSuccess).Select(r => r.Value).ToList();

        var images = d.Images.Select(v => 
        {
            return ProductImage.Reconstitute(v.Id, v.Url, v.AltText, v.IsPrimary, v.SortOrder, v.AddedAt);
        }).ToList();

        var variants = d.Variants.Select(v =>
        {
            var vPrice = Money.Create(v.Price.Amount, v.Price.Currency).Value;
            return ProductVariant.Reconstitute(v.Id, v.Name, v.Sku, vPrice, v.Stock, v.IsActive, v.Options);
        }).ToList();



        // Reconstitute bypasses Product.Create() — no domain events raised on load
        return Product.Reconstitute(
            id, d.Name, d.Description, slug, sku,
            price, salePrice, d.Stock, d.IsActive, d.IsFeatured,
            categoryId, dimensions, d.Attributes,
            tags, images, variants,
            d.AverageRating, d.ReviewCount,
            d.CreatedAt, d.UpdatedAt);
    }
}
