namespace MicroMart.ProductCatalog.Api.Requests.Products;
// PATCH /api/v1/products/{id}/price
public sealed record ChangePriceRequest(decimal Amount, string Currency);