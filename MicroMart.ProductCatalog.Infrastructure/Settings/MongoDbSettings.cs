
namespace MicroMart.ProductCatalog.Infrastructure.Settings;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "MicroMartProductCatalog";
    public string ProductsCollectionName { get; set; } = "products";
    public string CategoriesCollectionName { get; set; } = "categories";
    public string OutboxCollectionName { get; set; } = "outbox_messages";
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int ServerSelectionTimeoutMs { get; set; } = 5000;
    public int ConnectTimeoutMs { get; set; } = 3000;
}
