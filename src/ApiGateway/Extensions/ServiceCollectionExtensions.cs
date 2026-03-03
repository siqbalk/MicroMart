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
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using System.Text;
using Yarp.ReverseProxy.Transforms;

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

        services.AddReverseProxy()
     .LoadFromConfig(configuration.GetSection("ReverseProxy"))
     .AddTransforms(transforms =>
     {
         transforms.AddRequestTransform(context =>
         {
             var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
             if (!string.IsNullOrEmpty(token))
             {
                 context.ProxyRequest.Headers.Remove("Authorization");
                 context.ProxyRequest.Headers.Add("Authorization", token);
             }
             return ValueTask.CompletedTask;
         });
     })
     .ConfigureHttpClient((context, handler) =>
     {
         handler.ConnectTimeout = TimeSpan.FromSeconds(30);
     });

        // Configure resilience for outgoing proxy calls
        services.AddHttpClient("proxy")
            .AddResilienceHandler("gateway-pipeline", builder =>
            {
                // 🔁 AddOutputCache
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
        // -------------------
        // 1️⃣ CORS
        // -------------------

        var allowedOrigins = configuration
                            .GetSection("Cors:AllowedOrigins")
                            .Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("GatewayCorsPolicy",
                builder => builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        // -------------------
        // 2️⃣ JWT Authentication (Entra ID / Azure AD)
        // -------------------
        var azureAdConfig = configuration.GetSection("Security:AzureAd");
        if (azureAdConfig.Exists())
        {
            var tenantId = azureAdConfig["TenantId"];
            var clientId = azureAdConfig["ClientId"]; // This should be the API Client ID (a2bca0f8...)
            var instance = azureAdConfig["Instance"] ?? "https://login.microsoftonline.com";
            var authority = $"{instance}/{tenantId}";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = clientId; // This must match the API's Application ID URI
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = clientId, // Explicitly set
                        ValidAudiences = new[] { clientId, $"api://{clientId}" } // Accept both formats
                    };

                    // Add event for debugging
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("Token successfully validated");
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            Console.WriteLine($"Challenge: {context.Error}, {context.ErrorDescription}");
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                // Default policy requires authentication
                options.FallbackPolicy = options.DefaultPolicy;

                // Example policies
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("roles", "Admin"));

                options.AddPolicy("UserOrAdmin", policy =>
                    policy.RequireClaim("roles", new[] { "User", "Admin" }));
            });
        }

        // -------------------
        // 3️⃣ API Key Authentication Service
        // -------------------
        services.AddSingleton<IApiKeyValidationService, ApiKeyValidationService>();

        // -------------------
        // 4️⃣ Rate Limiting
        // -------------------
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

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