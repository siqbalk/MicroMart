using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace MicroMart.ApiGateway.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or create correlation ID
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Store in HttpContext for later use
        context.Items["CorrelationId"] = correlationId;

        // Add to Activity for distributed tracing
        Activity.Current?.SetTag("correlation.id", correlationId);

        await _next(context);
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue))
        {
            return headerValue.ToString();
        }

        // Try to get from request context
        if (context.Items.TryGetValue(CorrelationIdHeader, out var item) 
            && item is string correlationId)
        {
            return correlationId;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}