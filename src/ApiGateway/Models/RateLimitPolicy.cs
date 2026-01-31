// Models/RateLimit/RateLimitPolicy.cs
namespace MicroMart.ApiGateway.Models.RateLimit;

public class RateLimitPolicy
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Period { get; set; } = "1m";
    public int Limit { get; set; } = 100;
    public QuotaExceededResponse QuotaExceededResponse { get; set; } = new();
    public List<string> ClientWhitelist { get; set; } = new();
}

public class QuotaExceededResponse
{
    public string ContentType { get; set; } = "application/json";
    public string Content { get; set; } = "{\"error\":\"Rate limit exceeded\"}";
    public int StatusCode { get; set; } = 429;
}

public class RateLimitConfig
{
    public List<RateLimitPolicy> Policies { get; set; } = new();
    public List<string> IpWhitelist { get; set; } = new();
    public bool EnableEndpointRateLimiting { get; set; } = true;
    public bool StackBlockedRequests { get; set; } = false;
    public string RealIpHeader { get; set; } = "X-Real-IP";
    public string ClientIdHeader { get; set; } = "X-Client-Id";
    public int HttpStatusCode { get; set; } = 429;
    public bool DisableRateLimitHeaders { get; set; } = false;
}