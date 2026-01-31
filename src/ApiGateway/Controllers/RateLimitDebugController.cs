using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/debug")]
public class RateLimitDebugController : ControllerBase
{
    private readonly IOptions<IpRateLimitOptions> _rateLimitOptions;
    private readonly IRateLimitConfiguration _rateLimitConfig;
    private readonly ILogger<RateLimitDebugController> _logger;

    public RateLimitDebugController(
        IOptions<IpRateLimitOptions> rateLimitOptions,
        IRateLimitConfiguration rateLimitConfig,
        ILogger<RateLimitDebugController> logger)
    {
        _rateLimitOptions = rateLimitOptions;
        _rateLimitConfig = rateLimitConfig;
        _logger = logger;
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var options = _rateLimitOptions.Value;

        var config = new
        {
            EnableEndpointRateLimiting = options.EnableEndpointRateLimiting,
            StackBlockedRequests = options.StackBlockedRequests,
            RealIpHeader = options.RealIpHeader,
            ClientIdHeader = options.ClientIdHeader,
            HttpStatusCode = options.HttpStatusCode,
            QuotaExceededMessage = options.QuotaExceededMessage,
            IpWhitelist = options.IpWhitelist,
            ClientWhitelist = options.ClientWhitelist,
            GeneralRules = options.GeneralRules?.Select(r => new
            {
                r.Endpoint,
                r.Limit,
                r.Period
            }).ToList() 
        };

        _logger.LogInformation("Rate Limit Configuration: {@Config}", config);

        return Ok(config);
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Test endpoint called from IP: {ClientIp}", clientIp);

        return Ok(new
        {
            message = "Test successful",
            timestamp = DateTime.UtcNow,
            clientIp = clientIp,
            isLocalhost = clientIp == "127.0.0.1" || clientIp == "::1"
        });
    }
}