namespace MicroMart.ProductCatalog.Api.Requests.Products;

// POST /api/v1/products/{id}/stock/add|deduct
public sealed record StockRequest(int Quantity);
