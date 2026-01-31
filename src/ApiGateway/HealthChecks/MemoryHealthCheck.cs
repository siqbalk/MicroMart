using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MicroMart.ApiGateway.HealthChecks;

public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocated = GC.GetTotalMemory(false);
        var maxMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var usedPercentage = (double)allocated / maxMemory * 100;

        var data = new Dictionary<string, object>
        {
            ["AllocatedBytes"] = allocated,
            ["MaxMemoryBytes"] = maxMemory,
            ["UsedPercentage"] = $"{usedPercentage:F2}%",
            ["Gen0Collections"] = GC.CollectionCount(0),
            ["Gen1Collections"] = GC.CollectionCount(1),
            ["Gen2Collections"] = GC.CollectionCount(2)
        };

        var status = usedPercentage > 90
            ? HealthStatus.Degraded
            : HealthStatus.Healthy;

        var description = status == HealthStatus.Degraded
            ? $"Memory usage is high: {usedPercentage:F2}%"
            : $"Memory usage: {usedPercentage:F2}%";

        return Task.FromResult(new HealthCheckResult(status, description, data: data));
    }
}