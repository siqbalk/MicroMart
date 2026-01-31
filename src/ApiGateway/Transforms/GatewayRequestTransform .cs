using Yarp.ReverseProxy.Transforms;

namespace MicroMart.ApiGateway.Transforms;

public class GatewayRequestTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var correlationId = context.HttpContext.Items["CorrelationId"] as string;

        if (!string.IsNullOrEmpty(correlationId))
        {
            context.ProxyRequest.Headers.TryAddWithoutValidation(
                "X-Correlation-Id",
                correlationId);
        }

        context.ProxyRequest.Headers.TryAddWithoutValidation(
            "X-Forwarded-For",
            context.HttpContext.Connection.RemoteIpAddress?.ToString());

        context.ProxyRequest.Headers.TryAddWithoutValidation(
            "X-Forwarded-Proto",
            context.HttpContext.Request.Scheme);

        context.ProxyRequest.Headers.TryAddWithoutValidation(
            "X-Forwarded-Host",
            context.HttpContext.Request.Host.Host);

        // Remove original host header
        context.ProxyRequest.Headers.Remove("Host");

        return ValueTask.CompletedTask;
    }
}
