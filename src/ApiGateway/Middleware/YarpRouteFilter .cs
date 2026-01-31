using Yarp.ReverseProxy.Configuration;

namespace MicroMart.ApiGateway.Middleware;

public class YarpRouteFilter : IProxyConfigFilter
{
    private readonly ILogger<YarpRouteFilter> _logger;

    public YarpRouteFilter(ILogger<YarpRouteFilter> logger)
    {
        _logger = logger;
    }

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        // No changes to clusters
        return new ValueTask<ClusterConfig>(cluster);
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, CancellationToken cancel)
    {
        // 🔧 CRITICAL: Ensure we're not creating routes for local paths
        var matchPath = route.Match?.Path ?? "";

        // Log what routes are being configured
        _logger.LogInformation("Configuring YARP route: {RouteId} for path: {Path}",
            route.RouteId, matchPath);

        // Ensure we don't accidentally create routes for swagger/health
        if (matchPath.Contains("swagger") || matchPath.Contains("health") || matchPath == "/")
        {
            _logger.LogWarning("Preventing YARP route for local path: {Path}", matchPath);
            // Return a route that will never match (empty path)
            return new ValueTask<RouteConfig>(new RouteConfig
            {
                RouteId = route.RouteId,
                Match = new RouteMatch { Path = "" }, // Empty path won't match anything
                ClusterId = route.ClusterId
            });
        }

        return new ValueTask<RouteConfig>(route);
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        // 🔧 CRITICAL: Prevent YARP from handling local routes

        var matchPath = route.Match?.Path ?? "";
        _logger.LogDebug("Configuring YARP route: {RouteId} for path: {Path}",
            route.RouteId, matchPath);

        // Check if this is a route we should block from YARP
        if (IsLocalRoute(matchPath))
        {
            _logger.LogWarning("Blocking YARP route for local path: {Path}", matchPath);

            // Return a route that will never match by using an impossible condition
            var blockedRoute = route with
            {
                Match = route.Match with
                {
                    Path = "/__blocked_by_filter__" + Guid.NewGuid().ToString("N") // Unique path that won't match
                }
            };

            return new ValueTask<RouteConfig>(blockedRoute);
        }

        // For API routes, ensure they have proper transforms
        if (matchPath.StartsWith("/api/"))
        {
            var transforms = new List<Dictionary<string, string>>();

            // Add path transform if not already present
            if (route.Transforms?.Any(t => t.ContainsKey("PathPattern")) != true)
            {
                transforms.Add(new Dictionary<string, string>
                {
                    ["PathPattern"] = "{**catch-all}"
                });
            }

            // Add our custom headers transform
            transforms.Add(new Dictionary<string, string>
            {
                ["RequestHeader"] = "X-Gateway",
                ["Set"] = "MicroMart-API-Gateway"
            });

            transforms.Add(new Dictionary<string, string>
            {
                ["RequestHeader"] = "X-Gateway-Version",
                ["Set"] = "1.0.0"
            });

            // Merge with existing transforms
            var allTransforms = route.Transforms?.ToList() ?? new List<IReadOnlyDictionary<string, string>>();
            allTransforms.AddRange(transforms);

            var updatedRoute = route with
            {
                Transforms = allTransforms
            };

            return new ValueTask<RouteConfig>(updatedRoute);
        }

        return new ValueTask<RouteConfig>(route);
    }

    private bool IsLocalRoute(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var localPaths = new[]
        {
            "/swagger",
            "/health",
            "/",
            "/favicon.ico"
        };

        // Check for Swagger static files
        var isSwaggerStaticFile = path.Contains("swagger-ui", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

        return localPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
               isSwaggerStaticFile;
    }
}