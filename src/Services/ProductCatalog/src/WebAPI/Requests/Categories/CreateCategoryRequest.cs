namespace MicroMart.ProductCatalog.Api.Requests.Categories;
// POST /api/v1/categories
public sealed record CreateCategoryRequest(
    string Name,
    string Description,
    Guid? ParentId = null,
    int DisplayOrder = 0);