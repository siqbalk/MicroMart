// Middleware/YarpBypassMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroMart.ApiGateway.Middleware;

public class YarpBypassMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<YarpBypassMiddleware> _logger;

    public YarpBypassMiddleware(
        RequestDelegate next,
        ILogger<YarpBypassMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 🔧 List of paths that should NEVER go through YARP
        var bypassPaths = new[]
        {
            "/swagger",
            "/favicon",
            "/health",
            "/"
        };

        // Check for swagger static files
        var isSwaggerStaticFile = path.Contains("swagger-ui") ||
                                 path.EndsWith(".css") ||
                                 path.EndsWith(".js") ||
                                 path.EndsWith(".png") ||
                                 path.EndsWith(".ico");

        if (bypassPaths.Any(p => path.StartsWith(p)) || isSwaggerStaticFile)
        {
            _logger.LogDebug("Bypassing YARP for path: {Path}", path);
            await _next(context);
            return;
        }

        // For API paths, continue through pipeline (will reach YARP)
        if (path.StartsWith("/api/"))
        {
            _logger.LogDebug("Routing through YARP for API path: {Path}", path);
            await _next(context);
            return;
        }

        // For unknown paths, bypass YARP (404 will be handled by ASP.NET Core)
        _logger.LogWarning("Unknown path bypassing YARP: {Path}", path);
        await _next(context);
    }
}