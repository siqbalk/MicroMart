using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;

namespace MicroMart.ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check requested");

        return Ok(new
        {
            Status = "Healthy",
            Service = "MicroMart API Gateway",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            Uptime = Environment.TickCount / 1000 // Convert to seconds
        });
    }

    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDetailed()
    {
        _logger.LogInformation("Detailed health check requested");

        var memoryInfo = GC.GetGCMemoryInfo();
        var process = Process.GetCurrentProcess();

          process = null;

        // Let any exception bubble up to global handler
        // (Process.Threads.Count can throw on some platforms)
        int threadCount = process.Threads.Count;

        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Memory = new
            {
                Allocated = GC.GetTotalMemory(false),
                MaxMemory = memoryInfo.TotalAvailableMemoryBytes,
                HeapSize = memoryInfo.HeapSizeBytes,
                MemoryLoad = memoryInfo.MemoryLoadBytes,
                UsedPercentage = memoryInfo.TotalAvailableMemoryBytes > 0
                    ? (double)GC.GetTotalMemory(false) / memoryInfo.TotalAvailableMemoryBytes * 100
                    : 0
            },
            Threads = new
            {
                ThreadCount = threadCount,
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                HandleCount = process.HandleCount
            },
            GC = new
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                IsServerGC = GCSettings.IsServerGC,
                LatencyMode = GCSettings.LatencyMode.ToString()
            },
            Environment = new
            {
                MachineName = Environment.MachineName,
                OS = Environment.OSVersion.VersionString,
                ProcessorCount = Environment.ProcessorCount,
                Runtime = RuntimeInformation.FrameworkDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture,
                OSArchitecture = RuntimeInformation.OSArchitecture
            },
            Network = new
            {
                HostName = Dns.GetHostName(),
                LocalIP = GetLocalIPAddress()
            }
        });
    }

    private string GetLocalIPAddress()
    {
        // Let exceptions bubble up - global handler will catch them
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList
            .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            ?.ToString() ?? "Unable to determine";
    }

    [HttpGet("services")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServicesHealth([FromServices] HttpClient httpClient)
    {
        var services = new[]
        {
            new { Name = "ProductCatalog", Url = "http://localhost:5001/health" },
            new { Name = "OrderManagement", Url = "http://localhost:5002/health" },
            new { Name = "PaymentProcessing", Url = "http://localhost:5003/health" },
            new { Name = "UserIdentity", Url = "http://localhost:5004/health" }
        };

        var results = new List<object>();
        var tasks = services.Select(async service =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await httpClient.GetAsync(service.Url, cts.Token);

            return new
            {
                Service = service.Name,
                Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                StatusCode = (int)response.StatusCode,
                ResponseTime = DateTime.UtcNow
            };
        });

        var taskResults = await Task.WhenAll(tasks);
        results.AddRange(taskResults);

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Services = results
        });
    }
}