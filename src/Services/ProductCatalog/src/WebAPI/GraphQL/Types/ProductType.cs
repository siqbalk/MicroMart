using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Api.GraphQL.Types;
public sealed class ProductType : ObjectType<ProductResponse>
{
    protected override void Configure(IObjectTypeDescriptor<ProductResponse> descriptor)
    {
        descriptor
            .Description("A product in the catalog.");

        descriptor.Field(p => p.Id)
            .Description("Unique product identifier.");

        descriptor.Field(p => p.Name)
            .Description("Display name of the product.");

        descriptor.Field(p => p.Slug)
            .Description("URL-friendly slug derived from the product name.");

        descriptor.Field(p => p.Sku)
            .Description("Stock Keeping Unit — unique inventory identifier.");

        descriptor.Field(p => p.Stock)
            .Description("Total available stock quantity.");

        descriptor.Field(p => p.IsInStock)
            .Description("True when stock > 0.");

        descriptor.Field(p => p.IsActive)
            .Description("Whether the product is visible to customers.");

        descriptor.Field(p => p.IsFeatured)
            .Description("Whether the product appears in featured sections.");

        descriptor.Field(p => p.Images)
            .Description("Ordered list of product images.");

        descriptor.Field(p => p.Tags)
            .Description("Searchable tags attached to this product.");

        descriptor.Field(p => p.Attributes)
            .Description("Freeform key-value attributes (color, material, etc.).");

        descriptor.Field(p => p.CategoryId)
            .Description("Id of the category this product belongs to.");

        descriptor.Field(p => p.AverageRating)
            .Description("Average customer rating (0–5).");

        descriptor.Field(p => p.CreatedAt)
            .Description("UTC timestamp when the product was created.");

        descriptor.Field(p => p.UpdatedAt)
            .Description("UTC timestamp of the last update.");
    }
}