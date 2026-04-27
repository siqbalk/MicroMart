namespace MicroMart.ProductCatalog.Api.Requests.Products;
// PATCH /api/v1/products/{id}/featured
public sealed record SetFeaturedRequest(bool IsFeatured);