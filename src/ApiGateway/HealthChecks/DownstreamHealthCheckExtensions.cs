using MicroMart.ApiGateway.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MicroMart.ApiGateway.Extensions;

public static class DownstreamHealthCheckExtensions
{
    public static IHealthChecksBuilder AddDownstreamHealthChecks(
        this IHealthChecksBuilder builder)
    {
        return builder
            .AddCheck<DownstreamHealthCheck>("downstream-services")
            .AddCheck<MemoryHealthCheck>("memory");
    }
}