using Azure.Storage.Blobs;
using Elastic.Clients.Elasticsearch;
using MassTransit;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Infrastructure.Caching.Redis;
using MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit;
using MicroMart.ProductCatalog.Infrastructure.Messaging.MassTransit.Consumers;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Repositories;
using MicroMart.ProductCatalog.Infrastructure.Search.Elasticsearch;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using MicroMart.ProductCatalog.Infrastructure.Storage.AzureBlob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
namespace MicroMart.ProductCatalog.Infrastructure;

public static class InfrastructureServiceExtensions
{
    // Called from WebApi's Program.cs:
    //   builder.Services.AddInfrastructureServices(builder.Configuration);
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services
            .AddMongoDb(config)
            .AddRedis(config)
            .AddElasticsearch(config)
            .AddMassTransitWithRabbitMq(config)
            .AddBlobStorage(config)
            .AddRepositoriesAndServices();

        return services;
    }

    // ── MongoDB ──────────────────────────────────────────────────────────
    private static IServiceCollection AddMongoDb(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDbSettings>(
            config.GetSection(MongoDbSettings.SectionName));

        // MongoDbContext is Singleton — MongoClient is thread-safe
        services.AddSingleton<MongoDbContext>();

        // Startup: create all indexes before app receives requests
        services.AddHostedService<MongoDbInitializer>();

        return services;
    }

    // ── Redis ────────────────────────────────────────────────────────────
    private static IServiceCollection AddRedis(
        this IServiceCollection services, IConfiguration config)
    {
        var redisSettings = config
            .GetSection(RedisSettings.SectionName)
            .Get<RedisSettings>() ?? new();

        services.Configure<RedisSettings>(
            config.GetSection(RedisSettings.SectionName));

        // IConnectionMultiplexer is a long-lived Singleton
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configOptions = ConfigurationOptions.Parse(redisSettings.ConnectionString);
            configOptions.ConnectRetry = redisSettings.ConnectRetry;       // e.g. 5
            configOptions.ConnectTimeout = 10000;                          // 10s
            configOptions.SyncTimeout = 10000;                             // 10s
            configOptions.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
            configOptions.KeepAlive = 180;                                 // optional
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Microsoft IDistributedCache (used by ASP.NET session etc.)
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration = redisSettings.ConnectionString;
            opts.InstanceName = redisSettings.InstanceName;
        });

        // Our own ICacheService — wraps Redis with logging + resilience
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<CacheInvalidationService>();

        return services;
    }

    // ── Elasticsearch ────────────────────────────────────────────────────
    private static IServiceCollection AddElasticsearch(
        this IServiceCollection services, IConfiguration config)
    {
        var esSettings = config
            .GetSection(ElasticsearchSettings.SectionName)
            .Get<ElasticsearchSettings>() ?? new();

        services.Configure<ElasticsearchSettings>(
            config.GetSection(ElasticsearchSettings.SectionName));

        services.AddSingleton<ElasticsearchClient>(_ =>
        {
            var nodeUri = new Uri(esSettings.Uri);
            var settings = new ElasticsearchClientSettings(nodeUri);

            if (!string.IsNullOrEmpty(esSettings.Username))
                settings.Authentication(new Elastic.Transport.BasicAuthentication(
                    esSettings.Username, esSettings.Password));

            return new ElasticsearchClient(settings);
        });

        services.AddSingleton<Application.Abstractions.IProductSearchService, ElasticsearchProductSearchService>();

        // Startup: create index with correct mappings
        services.AddHostedService<ElasticsearchIndexInitializer>();

        return services;
    }

    // ── MassTransit + RabbitMQ ───────────────────────────────────────────
    private static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services, IConfiguration config)
    {
        var rabbitMqSettings = config
            .GetSection(RabbitMqSettings.SectionName)
            .Get<RabbitMqSettings>() ?? new();

        services.Configure<RabbitMqSettings>(
            config.GetSection(RabbitMqSettings.SectionName));

        services.AddMassTransit(mt =>
        {
            mt.AddConsumer<StockUpdatedConsumer>();
            mt.AddConsumer<ProductDeletedConsumer>();

            mt.UsingRabbitMq((ctx, rmq) =>
            {
            
                rmq.Host(rabbitMqSettings.Host,
                    rabbitMqSettings.VirtualHost, h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                // Global exponential retry: 3 attempts over ~30 seconds
                rmq.UseMessageRetry(r => r
                    .Exponential(
                        rabbitMqSettings.RetryCount,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(rabbitMqSettings.RetryIntervalSeconds)));

                rmq.ReceiveEndpoint("product-catalog-stock-updated", ep =>
                    ep.ConfigureConsumer<StockUpdatedConsumer>(ctx));

                rmq.ReceiveEndpoint("product-catalog-product-deleted", ep =>
                    ep.ConfigureConsumer<ProductDeletedConsumer>(ctx));

            });
        });

        // Outbox background processor — publishes pending messages to RabbitMQ
        services.AddHostedService<OutboxProcessorService>();

        return services;
    }

    // ── Azure Blob Storage ───────────────────────────────────────────────
    private static IServiceCollection AddBlobStorage(
        this IServiceCollection services, IConfiguration config)
    {
        var blobSettings = config
            .GetSection(BlobStorageSettings.SectionName)
            .Get<BlobStorageSettings>() ?? new();

        services.Configure<BlobStorageSettings>(
            config.GetSection(BlobStorageSettings.SectionName));

        services.AddSingleton<BlobServiceClient>(_ =>
            new BlobServiceClient(blobSettings.ConnectionString));

        services.AddScoped<IImageStorageService, AzureBlobImageStorageService>();

        return services;
    }

    // ── Repositories + Domain Services ──────────────────────────────────
    private static IServiceCollection AddRepositoriesAndServices(
        this IServiceCollection services)
    {
        // Repositories: Scoped — new instance per HTTP request
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // UnitOfWork: Scoped — shares the same request lifetime as repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}