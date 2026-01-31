namespace MicroMart.ApiGateway.Services;

public interface IApiKeyValidationService
{
    bool IsValidApiKey(string apiKey);
}

public class ApiKeyValidationService : IApiKeyValidationService
{
    private readonly IConfiguration _configuration;

    public ApiKeyValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsValidApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        var validApiKeys = _configuration.GetSection("Security:ApiKeys")
            .Get<List<string>>() ?? new List<string>();

        return validApiKeys.Contains(apiKey);
    }
}