using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Interfaces;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken ct);
    Task DeleteAsync(CategoryId id, CancellationToken ct);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct);
    Task<Category?> FindByIdAsync(CategoryId id, CancellationToken ct);
    Task<Category?> FindByIdWithSubcategoriesAsync(CategoryId id, CancellationToken ct);
    Task<Category?> FindBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoryId>> GetAllDescendantIdsAsync(CategoryId rootId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetSubCategoriesAsync(CategoryId parentId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetTopLevelAsync(CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
}
