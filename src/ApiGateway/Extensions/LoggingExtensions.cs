using MicroMart.ApiGateway.Middleware;
using Microsoft.Extensions.Logging;

namespace MicroMart.ApiGateway.Extensions;

public static class LoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Serilog if you're using it
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }
}