using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace MicroMart.ApiGateway.Services;

public class RateLimitPolicyProvider : IRateLimitConfiguration
{
    private readonly IpRateLimitOptions _ipOptions;
    private readonly ILogger<RateLimitPolicyProvider> _logger;
    private readonly IConfiguration _configuration;

    public RateLimitPolicyProvider(
    IOptions<IpRateLimitOptions> ipOptions,
    ILogger<RateLimitPolicyProvider> logger,
    IConfiguration configuration)
    {
        _ipOptions = ipOptions.Value;
        _logger = logger;
        _configuration = configuration;

        _logger.LogInformation("Rate limit configuration loaded:");
        _logger.LogInformation("EnableEndpointRateLimiting: {Enable}", _ipOptions.EnableEndpointRateLimiting);
        _logger.LogInformation("GeneralRules count: {Count}", _ipOptions.GeneralRules?.Count ?? 0);

        foreach (var rule in _ipOptions.GeneralRules ?? new List<RateLimitRule>())
        {
            _logger.LogInformation("Rule: {Endpoint} - {Limit}/{Period}",
                rule.Endpoint, rule.Limit, rule.Period);
        }

        // ✅ Load custom response messages from configuration
        LoadCustomResponseMessages();
    }

    public IList<IIpResolveContributor> IpResolvers
    {
        get
        {
            // ✅ Use RealIpHeader from configuration
            var realIpHeader = _ipOptions.RealIpHeader;
            return new List<IIpResolveContributor>
            {
                new IpHeaderResolveContributor(realIpHeader)
            };
        }
    }

    public IList<IClientResolveContributor> ClientResolvers
    {
        get
        {
            // ✅ Use ClientIdHeader from configuration
            var clientIdHeader = _ipOptions.ClientIdHeader;
            return new List<IClientResolveContributor>
            {
                new ClientHeaderResolveContributor(clientIdHeader)
            };
        }
    }

    public ICounterKeyBuilder EndpointCounterKeyBuilder => new CustomEndpointCounterKeyBuilder();
    public Func<double> RateIncrementer => () => 1.0;

    // ✅ Load custom response messages for different endpoints
    private void LoadCustomResponseMessages()
    {
        var rateLimitPolicies = _configuration.GetSection("IpRateLimiting");

        if (rateLimitPolicies.Exists())
        {
            foreach (var policySection in rateLimitPolicies.GetChildren())
            {
                if (policySection.Key.EndsWith("Policy") && policySection.Key != "GlobalPolicy")
                {
                    var endpoint = policySection.GetValue<string>("Endpoint");
                    var responseContent = policySection.GetSection("QuotaExceededResponse:Content").Get<string>();

                    if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(responseContent))
                    {
                        // Store custom response messages (you might need a dictionary field)
                        _logger.LogInformation("Loaded custom response for endpoint: {Endpoint}", endpoint);
                    }
                }
            }
        }
    }

    // ✅ Get custom response message for specific endpoint
    public string GetCustomResponseMessage(string endpoint)
    {
        // Logic to return custom message based on endpoint pattern matching
        if (endpoint.Contains("/api/products/"))
        {
            return "{\"error\":\"Product API rate limit exceeded. Max 50 requests per minute.\"}";
        }
        else if (endpoint.Contains("/api/orders/"))
        {
            return "{\"error\":\"Order API rate limit exceeded. Max 30 requests per minute.\"}";
        }
        else if (endpoint.Contains("/api/payments/"))
        {
            return "{\"error\":\"Payment API rate limit exceeded. Max 20 requests per minute.\"}";
        }
        else if (endpoint.Contains("/api/auth/"))
        {
            return "{\"error\":\"Authentication rate limit exceeded. Max 10 requests per minute.\"}";
        }

        return _ipOptions.QuotaExceededMessage;
    }

    public void RegisterResolvers()
    {
       
    }
}

// ✅ Enhanced Counter Key Builder
public class CustomEndpointCounterKeyBuilder : ICounterKeyBuilder
{
    public string Build(ClientRequest request, RateLimitRule rule)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Key format: [ClientId]_[ClientIp]_[HttpVerb]_[Path]
        var keyParts = new List<string>();

        // Add client ID if available
        if (!string.IsNullOrEmpty(request.ClientId))
        {
            keyParts.Add($"client:{request.ClientId}");
        }

        // Add client IP
        if (!string.IsNullOrEmpty(request.ClientIp))
        {
            keyParts.Add($"ip:{request.ClientIp}");
        }

        // Add HTTP verb
        keyParts.Add($"verb:{request.HttpVerb}");

        // Add path (normalized)
        var normalizedPath = NormalizePath(request.Path);
        keyParts.Add($"path:{normalizedPath}");

        return string.Join("_", keyParts);
    }

    private string NormalizePath(string path)
    {
        // Normalize the path (remove trailing slashes, etc.)
        return path?.Trim('/').ToLowerInvariant() ?? string.Empty;
    }

    public string Build(ClientRequestIdentity requestIdentity, RateLimitRule rule)
    {
        // Implementation for ClientRequestIdentity
        var keyParts = new List<string>();

        if (!string.IsNullOrEmpty(requestIdentity.ClientId))
        {
            keyParts.Add($"client:{requestIdentity.ClientId}");
        }

        keyParts.Add($"ip:{requestIdentity.ClientIp}");
        keyParts.Add($"verb:{requestIdentity.HttpVerb}");

        var normalizedPath = NormalizePath(requestIdentity.Path);
        keyParts.Add($"path:{normalizedPath}");

        return string.Join("_", keyParts);
    }
}

public class ClientRequest
{
    public string ClientIp { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpVerb { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}