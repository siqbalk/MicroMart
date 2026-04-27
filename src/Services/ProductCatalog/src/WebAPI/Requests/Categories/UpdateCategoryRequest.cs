namespace MicroMart.ProductCatalog.Api.Requests.Categories;
// PUT /api/v1/categories/{id}
public sealed record UpdateCategoryRequest(
    string Name,
    string Description,
    string? ImageUrl = null,
    int DisplayOrder = 0);
