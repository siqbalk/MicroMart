using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Transforms;

namespace MicroMart.ApiGateway.Middleware;

public class RequestTransformMiddleware : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var correlationId = context.HttpContext.Items["CorrelationId"] as string;

        if (!string.IsNullOrEmpty(correlationId))
        {
            // Forward correlation ID to downstream services
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        // Add additional headers for downstream services
        context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For",
            context.HttpContext.Connection.RemoteIpAddress?.ToString());
        context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Proto",
            context.HttpContext.Request.Scheme);
        context.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Host",
            context.HttpContext.Request.Host.Host);

        // Remove the original host header to avoid issues
        context.ProxyRequest.Headers.Remove("Host");

        return default;
    }
}