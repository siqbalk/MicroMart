using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MicroMart.ApiGateway.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration.TotalSeconds,
            Timestamp = DateTime.UtcNow,
            Services = report.Entries.Select(e => new
            {
                Service = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration.TotalSeconds,
                Data = e.Value.Data,
                Exception = e.Value.Exception?.Message,
                Tags = e.Value.Tags
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = context.Request.Path.Value?.Contains("detailed") == true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}