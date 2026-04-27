using MicroMart.ProductCatalog.Application.DTOs;

namespace MicroMart.ProductCatalog.Api.GraphQL.Types;
public sealed class PagedResultType<T> : ObjectType<PagedResponse<T>>
    where T : class
{
    protected override void Configure(IObjectTypeDescriptor<PagedResponse<T>> descriptor)
    {
        descriptor.Field(p => p.Items).Description("Current page items.");
        descriptor.Field(p => p.Page).Description("Current page number (1-based).");
        descriptor.Field(p => p.PageSize).Description("Requested page size.");
        descriptor.Field(p => p.TotalCount).Description("Total items across all pages.");
        descriptor.Field(p => p.TotalPages).Description("Total pages = ceil(TotalCount / PageSize).");
        descriptor.Field(p => p.HasNextPage).Description("True when more pages follow.");
        descriptor.Field(p => p.HasPrevPage).Description("True when previous pages exist.");
    }
}