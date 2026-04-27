

using MicroMart.ProductCatalog.Domain.Events;
using MicroMart.ProductCatalog.Domain.ValueObjects;
using MicroMart.Shared.Core.Primitives;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Domain.Entities;

public sealed class Product : AggregateRoot<ProductId>
{
    // ── Private backing fields ───────────────────────────────
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariant> _variants = [];
    private readonly List<Tag> _tags = [];

    // ── Properties — all private setters ────────────────────
    // Nobody outside the aggregate can mutate state directly
    // All changes go through methods below
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = default!;
    public Sku Sku { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Money? SalePrice { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public CategoryId CategoryId { get; private set; } = default!;
    public Dimensions Dimensions { get; private set; } = Dimensions.Empty;
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Flexible attributes — "RAM": "16GB", "Color": "Space Gray"
    // Different per category — no fixed schema
    public Dictionary<string, string> Attributes { get; private set; } = [];

    // Read-only collections exposed — nobody can add directly
    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyList<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // Computed properties
    public bool IsOnSale => SalePrice is not null;
    public bool IsInStock => Stock > 0;
    public bool IsLowStock => Stock > 0 && Stock <= 10;
    public Money EffectivePrice => SalePrice ?? Price;

    // ── Private constructor ───────────────────────────────────
    // Forces all creation through the Create() factory method
    // This ensures business rules are ALWAYS checked on creation
    private Product() { }

    // ── Factory Method ────────────────────────────────────────
    // Static factory — returns Result instead of throwing
    // All validation here — if it passes, object is ALWAYS valid
    public static Result<Product> Create(
        string name,
        string description,
        Sku sku,
        Money price,
        int initialStock,
        CategoryId categoryId,
        Dimensions dimensions,
        Dictionary<string, string>? attributes = null)
    {
        // ── Business Rules ──────────────────────────────────────
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(
                Error.Validation("Product", "Product name is required"));

        if (name.Length > 200)
            return Result.Failure<Product>(
                Error.Validation("Product", "Product name cannot exceed 200 characters"));

        if (initialStock < 0)
            return Result.Failure<Product>(
                Error.Validation("Product", "Initial stock cannot be negative"));

        if (price.Amount <= 0)
            return Result.Failure<Product>(
                Error.Validation("Product", "Price must be greater than zero"));

        var slugResult = Slug.Create(name);
        if (slugResult.IsFailure)
            return Result.Failure<Product>(slugResult.Error);

        var product = new Product
        {
            Id = ProductId.New(),
            Name = name.Trim(),
            Description = description.Trim(),
            Slug = slugResult.Value,
            Sku = sku,
            Price = price,
            Stock = initialStock,
            CategoryId = categoryId,
            Dimensions = dimensions,
            Attributes = attributes ?? [],
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Raise domain event — dispatched by Infrastructure after save
        product.Raise(new ProductCreatedEvent(
            product.Id,
            product.Name,
            product.Sku.Value,
            product.Price,
            product.CategoryId,
            product.Stock));

        return Result.Success(product);
    }

    // ── Behaviour Methods ─────────────────────────────────────

    public Result UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(
                Error.Validation("Product", "Name is required"));

        var slugResult = Slug.Create(name);
        if (slugResult.IsFailure) return Result.Failure(slugResult.Error);

        Name = name.Trim();
        Description = description.Trim();
        Slug = slugResult.Value;
        UpdatedAt = DateTime.UtcNow;

        Raise(new ProductUpdatedEvent(Id, Name));
        return Result.Success();
    }

    public Result ChangePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            return Result.Failure(
                Error.Validation("Product", "Price must be greater than zero"));

        if (newPrice.Currency != Price.Currency)
            return Result.Failure(
                Error.Validation("Product", "Cannot change currency of an existing product"));

        var oldPrice = Price;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        Raise(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
        return Result.Success();
    }

    public Result SetSalePrice(Money salePrice)
    {
        if (salePrice.Currency != Price.Currency)
            return Result.Failure(
                Error.Validation("Product", "Sale price currency must match product currency"));

        if (!salePrice.IsGreaterThan(Money.Zero(Price.Currency)))
            return Result.Failure(
                Error.Validation("Product", "Sale price must be greater than zero"));

        if (salePrice.IsGreaterThan(Price))
            return Result.Failure(
                Error.Validation("Product", "Sale price cannot be higher than regular price"));

        SalePrice = salePrice;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveSalePrice()
    {
        SalePrice = null;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(
                Error.Validation("Product", "Quantity must be greater than zero"));

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;

        Raise(new StockUpdatedEvent(Id, Stock, quantity, "ADD"));
        return Result.Success();
    }

    public Result DeductStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure(
                Error.Validation("Product", "Quantity must be greater than zero"));

        if (quantity > Stock)
            return Result.Failure(
                Error.Validation("Product",
                    $"Insufficient stock. Requested: {quantity}, Available: {Stock}"));

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;

        Raise(new StockUpdatedEvent(Id, Stock, quantity, "DEDUCT"));
        return Result.Success();
    }

    public Result AddImage(string url, string altText, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure(
                Error.Validation("Product", "Image URL is required"));

        if (_images.Count >= 10)
            return Result.Failure(
                Error.Validation("Product", "Maximum 10 images per product"));

        // If this image is primary, demote all others
        if (isPrimary)
            _images.ForEach(i => i.SetAsPrimary(false));

        // First image is always primary
        if (!_images.Any()) isPrimary = true;

        var image = ProductImage.Create(url, altText, isPrimary);
        _images.Add(image);
        UpdatedAt = DateTime.UtcNow;

        Raise(new ProductImageAddedEvent(Id, url, isPrimary));
        return Result.Success();
    }

    public Result RemoveImage(string url)
    {
        var image = _images.FirstOrDefault(i => i.Url == url);
        if (image is null)
            return Result.Failure(
                Error.NotFound("ProductImage", url));

        _images.Remove(image);

        // If we removed the primary, make the first remaining image primary
        if (image.IsPrimary && _images.Any())
            _images[0].SetAsPrimary(true);

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddTag(string tagValue)
    {
        var tagResult = Tag.Create(tagValue);
        if (tagResult.IsFailure) return Result.Failure(tagResult.Error);

        if (_tags.Any(t => t.Value == tagResult.Value.Value))
            return Result.Success(); // idempotent — no error if already exists

        if (_tags.Count >= 20)
            return Result.Failure(
                Error.Validation("Product", "Maximum 20 tags per product"));

        _tags.Add(tagResult.Value);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddVariant(
        string name, string sku, Money price, int stock)
    {
        if (_variants.Any(v => v.Sku == sku))
            return Result.Failure(
                Error.Conflict("ProductVariant",
                    $"Variant with SKU {sku} already exists"));

        var variantResult = ProductVariant.Create(name, sku, price, stock);
        if (variantResult.IsFailure) return Result.Failure(variantResult.Error);

        _variants.Add(variantResult.Value);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure(
                Error.Validation("Product", "Product is already active"));

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        Raise(new ProductActivatedEvent(Id, Name));
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(
                Error.Validation("Product", "Product is already inactive"));

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Raise(new ProductDeactivatedEvent(Id, Name));
        return Result.Success();
    }

    public void SetFeatured(bool featured)
    {
        IsFeatured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result UpdateAttributes(Dictionary<string, string> attributes)
    {
        if (attributes.Count > 50)
            return Result.Failure(
                Error.Validation("Product", "Maximum 50 attributes per product"));

        Attributes = attributes;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Called by Review Service integration event handler
    public void UpdateRating(decimal averageRating, int reviewCount)
    {
        AverageRating = Math.Round(averageRating, 1);
        ReviewCount = reviewCount;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result UpdateDimensions(Dimensions dimensions)
    {
        Dimensions = dimensions;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public static Product Reconstitute(
    ProductId id,
    string name,
    string description,
    Slug slug,
    Sku sku,
    Money price,
    Money? salePrice,
    int stock,
    bool isActive,
    bool isFeatured,
    CategoryId categoryId,
    Dimensions dimensions,
    Dictionary<string, string> attributes,
    List<Tag> tags,
    List<ProductImage> images,
    List<ProductVariant> variants,
    decimal averageRating,
    int reviewCount,
    DateTime createdAt,
    DateTime updatedAt)
    {
        var product = new Product
        {
            Id = id,
            Name = name,
            Description = description,
            Slug = slug,
            Sku = sku,
            Price = price,
            SalePrice = salePrice,
            Stock = stock,
            IsActive = isActive,
            IsFeatured = isFeatured,
            CategoryId = categoryId,
            Dimensions = dimensions,
            Attributes = attributes,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Populate the private backing fields directly
        product._tags.AddRange(tags);
        product._images.AddRange(images);
        product._variants.AddRange(variants);

        return product;
    }
}
