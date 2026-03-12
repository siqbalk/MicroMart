using FluentValidation;
using Mapster;
using MapsterMapper;
using MicroMart.ProductCatalog.Application.Behaviours;
using Microsoft.Extensions.DependencyInjection;
namespace MicroMart.ProductCatalog.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // ── MediatR — registers ALL handlers in this assembly ────────────
        // Open behaviours apply to ALL request/response type pairs
        // Order matters — behaviours run in registration order
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(
                typeof(ApplicationServiceExtensions).Assembly);

            // 1. Logging — FIRST: logs before validation so we see all requests
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));

            // 2. Validation — stops bad requests before hitting the handler
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));

            // 3. Caching — returns cached result if available (queries only)
            //    Only applies if TRequest implements ICacheableQuery
            cfg.AddOpenBehavior(typeof(CachingBehaviour<,>));

            // 4. Performance — warns if handler is too slow (LAST: measures
            //    actual handler time, not validation/cache overhead)
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });

        // ── FluentValidation — registers ALL validators in this assembly ──
        // Scanned and registered automatically — no need to add individually
        services.AddValidatorsFromAssembly(
            typeof(ApplicationServiceExtensions).Assembly,
            includeInternalTypes: true);

        // ── Mapster — configure all mappings ──────────────────────────────
        // IRegister implementations scanned and applied automatically
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(ApplicationServiceExtensions).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}

// ── Program.cs (WebApi) — how to call this ────────────────────────────
// builder.Services.AddApplicationServices();
// builder.Services.AddInfrastructureServices(builder.Configuration);
//
// The WebApi layer calls both — Application layer knows nothing
// about how Infrastructure registers its implementations.