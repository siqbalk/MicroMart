using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MicroMart.ApiGateway.HealthChecks;

public class DownstreamHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownstreamHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public DownstreamHealthCheck(
        HttpClient httpClient,
        ILogger<DownstreamHealthCheck> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Define the services to check
            var services = new[]
            {
                new { Name = "ProductCatalog", Url = "http://localhost:5001/health", Required = true },
                new { Name = "OrderManagement", Url = "http://localhost:5002/health", Required = true },
                new { Name = "PaymentProcessing", Url = "http://localhost:5003/health", Required = true },
                new { Name = "UserIdentity", Url = "http://localhost:5004/health", Required = true },
                new { Name = "ReviewService", Url = "http://localhost:5006/health", Required = false }
            };

            var results = new Dictionary<string, object>();
            var unhealthyServices = new List<string>();
            var degradedServices = new List<string>();

            // Check each service
            foreach (var service in services)
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // Create request with timeout
                    using var request = new HttpRequestMessage(HttpMethod.Get, service.Url);
                    request.Headers.Add("User-Agent", "MicroMart-API-Gateway-HealthCheck");

                    // Set timeout for this individual check
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var response = await _httpClient.SendAsync(request, linkedCts.Token);
                    stopwatch.Stop();

                    var isHealthy = response.IsSuccessStatusCode;
                    var responseTime = stopwatch.ElapsedMilliseconds;
                    var isSlow = responseTime > 1000; // Slow if > 1 second

                    results[service.Name] = new
                    {
                        Status = isHealthy ? "Healthy" : "Unhealthy",
                        StatusCode = (int)response.StatusCode,
                        ResponseTimeMs = responseTime,
                        IsSlow = isSlow,
                        Timestamp = DateTime.UtcNow
                    };

                    if (!isHealthy)
                    {
                        unhealthyServices.Add(service.Name);
                        _logger.LogWarning(
                            "Service {ServiceName} is unhealthy. Status: {StatusCode}, ResponseTime: {ResponseTime}ms",
                            service.Name, response.StatusCode, responseTime);
                    }
                    else if (isSlow)
                    {
                        degradedServices.Add(service.Name);
                        _logger.LogWarning(
                            "Service {ServiceName} is slow. Response time: {ResponseTime}ms",
                            service.Name, responseTime);
                    }
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout occurred
                    results[service.Name] = new
                    {
                        Status = "Unhealthy",
                        StatusCode = 408,
                        ResponseTimeMs = 3000,
                        IsSlow = true,
                        Timestamp = DateTime.UtcNow,
                        Error = "Health check timeout"
                    };

                    unhealthyServices.Add(service.Name);
                    _logger.LogWarning("Health check timeout for service {ServiceName}", service.Name);
                }
                catch (Exception ex)
                {
                    // Other errors (connection refused, etc.)
                    results[service.Name] = new
                    {
                        Status = "Unhealthy",
                        StatusCode = 0,
                        ResponseTimeMs = 0,
                        IsSlow = false,
                        Timestamp = DateTime.UtcNow,
                        Error = ex.Message
                    };

                    unhealthyServices.Add(service.Name);
                    _logger.LogError(ex, "Health check failed for service {ServiceName}", service.Name);
                }
            }

            // Determine overall status
            var requiredServices = services.Where(s => s.Required).ToList();
            var unhealthyRequired = services
                .Where(s => s.Required && unhealthyServices.Contains(s.Name))
                .ToList();

            HealthStatus overallStatus;
            string description;

            if (unhealthyRequired.Any())
            {
                // Any required service is unhealthy
                overallStatus = HealthStatus.Unhealthy;
                description = $"Unhealthy services: {string.Join(", ", unhealthyRequired.Select(s => s.Name))}";
            }
            else if (degradedServices.Any())
            {
                // Some services are slow but not unhealthy
                overallStatus = HealthStatus.Degraded;
                description = $"Degraded services: {string.Join(", ", degradedServices)}";
            }
            else
            {
                // All services are healthy
                overallStatus = HealthStatus.Healthy;
                description = $"All {services.Length} downstream services are healthy";
            }

            return new HealthCheckResult(overallStatus, description, data: results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check downstream services health");
            return HealthCheckResult.Unhealthy(
                "Failed to check downstream services health",
                exception: ex);
        }
    }
}