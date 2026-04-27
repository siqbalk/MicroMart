using Asp.Versioning;
using MicroMart.ProductCatalog.Api.GraphQL;
using MicroMart.ProductCatalog.Api.GraphQL.Mutations;
using MicroMart.ProductCatalog.Api.GraphQL.Queries;
using MicroMart.ProductCatalog.Api.GraphQL.Types;
using MicroMart.ProductCatalog.Application.DTOs;

namespace MicroMart.ProductCatalog.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Minimal API support ─────────────────────────────────
        services.AddEndpointsApiExplorer();

        // ── API Versioning for route groups ─────────────────────
        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
            o.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"));
        });

        // ── Swagger ──────────────────────────────────────────────
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "MicroMart Product Catalog", Version = "v1" });
        });

        // ── CORS ─────────────────────────────────────────────────
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddPolicy("Default", p => p
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        // ── Health Checks ─────────────────────────────────────────
        services.AddHealthChecks()
            .AddMongoDb(config["MongoDB:ConnectionString"]!, name: "mongodb", tags: ["db"])
            .AddRedis(config["Redis:ConnectionString"]!, name: "redis", tags: ["cache"])
            .AddElasticsearch(config["Elasticsearch:Uri"]!, name: "elasticsearch", tags: ["search"]);

        // ── Hot Chocolate GraphQL ─────────────────────────────────
        var gqlCfg = config.GetSection("GraphQL");

        services
       .AddGraphQLServer()

       // Object types
       .AddType<ProductType>()
       .AddType<CategoryType>()
       .AddType<PagedResultType<ProductResponse>>()

       // Query + Mutation
       .AddQueryType()
       .AddTypeExtension<ProductQueries>()
       .AddTypeExtension<CategoryQueries>()
       .AddMutationType()
       .AddTypeExtension<ProductMutations>()
       .AddTypeExtension<CategoryMutations>()

       // Features
       .AddFiltering()
       .AddSorting()
       .AddProjections()

       // ✅ ADD THIS
       .AddErrorFilter<GraphQLErrorFilter>()

       // Security
       .AddMaxExecutionDepthRule(gqlCfg.GetValue<int>("MaxAllowedDepth", 10))

       // Options
       .ModifyRequestOptions(o =>
           o.IncludeExceptionDetails = gqlCfg.GetValue<bool>("EnableSchemaIntrospection", true));

        services.AddProblemDetails();

        return services;
    }
}