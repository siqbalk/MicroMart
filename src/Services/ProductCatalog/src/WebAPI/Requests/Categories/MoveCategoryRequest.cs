namespace MicroMart.ProductCatalog.Api.Requests.Categories;
// PUT /api/v1/categories/{id}/move — null = move to root
public sealed record MoveCategoryRequest(Guid? NewParentId);