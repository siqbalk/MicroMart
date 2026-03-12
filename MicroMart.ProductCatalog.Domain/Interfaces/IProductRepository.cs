using MicroMart.ProductCatalog.Domain.Entities;
using MicroMart.ProductCatalog.Domain.ValueObjects;

namespace MicroMart.ProductCatalog.Domain.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken ct);
    Task<long> CountByCategoryAsync(CategoryId categoryId, CancellationToken ct);
    Task DeleteAsync(ProductId id, CancellationToken ct);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct);
    Task<Product?> FindByIdAsync(ProductId id, CancellationToken ct);
    Task<Product?> FindBySkuAsync(string sku, CancellationToken ct);
    Task<Product?> FindBySlugAsync(string slug, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<ProductId> ids, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int count, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold, CancellationToken ct);
    Task<(IReadOnlyList<Product> Items, long TotalCount)> GetPagedAsync(int page, int pageSize, string? categoryId, bool? isActive, bool? isFeatured, string? sortBy, bool ascending, CancellationToken ct);
    Task UpdateAsync(Product product, CancellationToken ct);
}
