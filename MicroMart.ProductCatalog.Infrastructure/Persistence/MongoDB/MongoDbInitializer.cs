using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;

public sealed class MongoDbInitializer(
    MongoDbContext context,
    ILogger<MongoDbInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        logger.LogInformation("MongoDB initializing — ensuring indexes exist...");
        try
        {
            await context.EnsureIndexesAsync(ct);
            logger.LogInformation("MongoDB initialization complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MongoDB initialization failed.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
