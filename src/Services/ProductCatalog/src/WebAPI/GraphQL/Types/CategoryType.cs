using MicroMart.ProductCatalog.Application.DTOs;

namespace MicroMart.ProductCatalog.Api.GraphQL.Types;
public sealed class CategoryType : ObjectType<CategoryResponse>
{
    protected override void Configure(IObjectTypeDescriptor<CategoryResponse> descriptor)
    {
        descriptor.Description("A product category — may have subcategories.");

        descriptor.Field(c => c.Id).Description("Unique category identifier.");
        descriptor.Field(c => c.Name).Description("Display name.");
        descriptor.Field(c => c.Slug).Description("URL-friendly slug.");
        descriptor.Field(c => c.Description).Description("Long description shown on category pages.");
        descriptor.Field(c => c.ParentId).Description("Parent category Id — null for top-level categories.");
        descriptor.Field(c => c.ImageUrl).Description("Optional category image URL.");
        descriptor.Field(c => c.DisplayOrder).Description("Sort order among siblings.");
    }
}

