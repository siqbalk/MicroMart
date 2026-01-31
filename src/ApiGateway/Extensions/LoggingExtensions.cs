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

            // Remove AddFile as it is not a built-in provider.
            // If you want file logging, consider using Serilog or another logging provider.
            // Example (if using Serilog):
            // Log.Logger = new LoggerConfiguration()
            //     .ReadFrom.Configuration(configuration)
            //     .WriteTo.File("logs/micromart-gateway-.txt", rollingInterval: RollingInterval.Day)
            //     .CreateLogger();
            // builder.AddSerilog();

            // If you want to keep file logging, you need to install a compatible provider
            // such as Serilog.Extensions.Logging.File or similar.
        });

        return services;
    }
}