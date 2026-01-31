using AspNetCoreRateLimit;
using MicroMart.ApiGateway.HealthChecks;
using MicroMart.ApiGateway.Middleware;
using MicroMart.ApiGateway.Models;
using MicroMart.ApiGateway.Services;
using MicroMart.ApiGateway.Transforms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using System.Text;
using Microsoft.OpenApi;

namespace MicroMart.ApiGateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(configuration.GetSection("Gateway"));
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // Add Output Caching
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
        });

        return services;
    }

    public static IServiceCollection AddReverseProxy(
     this IServiceCollection services,
     IConfiguration configuration)
    {
        services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddConfigFilter<YarpRouteFilter>()
            .AddTransforms<GatewayTransformProvider>();

        // Configure resilience for outgoing proxy calls
        services.AddHttpClient("proxy")
            .AddResilienceHandler("gateway-pipeline", builder =>
            {
                // 🔁 Retry
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential
                });

                // ⏱ Timeout
                builder.AddTimeout(TimeSpan.FromSeconds(30));

                // 🔥 Circuit Breaker
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,                 // 50% failures
                    MinimumThroughput = 10,             // Minimum requests before evaluating
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                });
            });

        return services;
    }


    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("GatewayCorsPolicy",
                builder => builder
                    .WithOrigins("http://localhost:3000", "http://localhost:8080")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        // JWT Authentication
        var jwtConfig = configuration.GetSection("Security:Jwt");
        if (jwtConfig.Exists())
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtConfig["Issuer"],
                        ValidAudience = jwtConfig["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtConfig["Secret"] ?? throw new ArgumentNullException()))
                    };
                });

            services.AddAuthorization();
        }

        // API Key Authentication Service
        services.AddSingleton<IApiKeyValidationService, ApiKeyValidationService>();

        // Rate Limiting

       
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddInMemoryRateLimiting();

        return services;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        // Add OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("micro-mart-api-gateway")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "development"
                }))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
            });

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
    .AddCheck<DownstreamHealthCheck>("downstream-services")
    .AddCheck<MemoryHealthCheck>("memory-health")
    .AddCheck("self", () => HealthCheckResult.Healthy("API Gateway is running"));

        return services;
    }

    public static WebApplicationBuilder AddConfigurationFiles(this WebApplicationBuilder builder)
    {

        var rateLimitingConfigPath = Path.Combine(builder.Environment.ContentRootPath, "Configuration", "RateLimiting", "rate-limits.json");
        builder.Configuration.AddJsonFile(rateLimitingConfigPath, optional: false, reloadOnChange: true);

        var routeConfigPath = Path.Combine(builder.Environment.ContentRootPath, "Configuration", "ReverseProxy", "routes.json");
        builder.Configuration.AddJsonFile(routeConfigPath, optional: false, reloadOnChange: true);

        //var healthCheckConfigPath = Path.Combine(builder.Environment.ContentRootPath, "Configuration", "HealthCheck", "health-check.json");
        //builder.Configuration.AddJsonFile(healthCheckConfigPath, optional: false, reloadOnChange: true);

        return builder;
    }

}