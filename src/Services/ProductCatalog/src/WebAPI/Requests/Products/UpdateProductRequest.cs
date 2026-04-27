namespace MicroMart.ProductCatalog.Api.Requests.Products;
// PUT /api/v1/products/{id}
public sealed record UpdateProductRequest(string Name, string Description);