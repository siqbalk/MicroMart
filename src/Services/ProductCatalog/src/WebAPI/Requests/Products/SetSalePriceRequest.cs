namespace MicroMart.ProductCatalog.Api.Requests.Products;

// PUT /api/v1/products/{id}/sale-price  — null Amount = remove sale price
public sealed record SetSalePriceRequest(decimal? Amount, string? Currency);