using MicroMart.ApiGateway.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing; // Add this using directive
using Microsoft.Extensions.DependencyInjection; // Add this using directive

namespace MicroMart.ApiGateway.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGatewayMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Move health checks mapping to IEndpointRouteBuilder

        app.UseHttpsRedirection();
        app.UseCors("GatewayCorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseOutputCache();

        return app;
    }
}