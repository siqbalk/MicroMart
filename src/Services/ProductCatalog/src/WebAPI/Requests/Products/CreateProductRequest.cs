namespace MicroMart.ProductCatalog.Api.Requests.Products;

// POST /api/v1/products
public sealed record CreateProductRequest(
    string Name,
    string Description,
    string Sku,
    decimal PriceAmount,
    string PriceCurrency,
    int InitialStock,
    string CategoryId,
    decimal WeightKg = 0,
    decimal LengthCm = 0,
    decimal WidthCm = 0,
    decimal HeightCm = 0,
    Dictionary<string, string>? Attributes = null,
     List<string>?  Tags = null);