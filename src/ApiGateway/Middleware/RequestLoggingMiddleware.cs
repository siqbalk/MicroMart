using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MicroMart.ApiGateway.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _logRequestResponse;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _logRequestResponse = configuration.GetValue<bool>("Logging:EnableRequestResponseLogging", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetCorrelationId(context);
        var requestId = Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.Items["CorrelationId"] = correlationId;

        try
        {
            await LogRequestAsync(context, correlationId, requestId);

            await _next(context); // ⚠️ IMPORTANT: Do NOT wrap response stream (YARP safe)

            stopwatch.Stop();

            await LogResponseAsync(
                context,
                correlationId,
                requestId,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogExceptionAsync(
                context,
                correlationId,
                requestId,
                stopwatch.ElapsedMilliseconds,
                ex);

            throw;
        }
    }

    // ----------------------------
    // Request Logging
    // ----------------------------

    private async Task LogRequestAsync(HttpContext context, string correlationId, string requestId)
    {
        var request = context.Request;

        var logData = new
        {
            Event = "RequestStarted",
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.Value,
            ClientIp = GetClientIp(context),
            UserAgent = request.Headers.UserAgent.ToString(),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            Body = await GetRequestBodyAsync(request)
        };

        _logger.LogInformation("Request started: {@Request}", logData);
    }

    // ----------------------------
    // Response Logging (YARP SAFE)
    // ----------------------------

    private Task LogResponseAsync(
        HttpContext context,
        string correlationId,
        string requestId,
        long elapsedMilliseconds)
    {
        var response = context.Response;

        var logData = new
        {
            Event = "RequestCompleted",
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            ElapsedMilliseconds = elapsedMilliseconds,
            Performance = GetPerformanceCategory(elapsedMilliseconds)
        };

        var logLevel = GetLogLevel(response.StatusCode);

        _logger.Log(logLevel, "Request completed: {@Response}", logData);

        if (elapsedMilliseconds > 1000)
        {
            _logger.LogWarning(
                "Slow request detected: {Path} took {ElapsedMs}ms",
                context.Request.Path,
                elapsedMilliseconds);
        }

        return Task.CompletedTask;
    }

    // ----------------------------
    // Exception Logging
    // ----------------------------

    private Task LogExceptionAsync(
        HttpContext context,
        string correlationId,
        string requestId,
        long elapsedMilliseconds,
        Exception exception)
    {
        var logData = new
        {
            Event = "RequestFailed",
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path,
            ElapsedMilliseconds = elapsedMilliseconds,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            InnerException = exception.InnerException?.Message
        };

        _logger.LogError(exception, "Request failed: {@Error}", logData);

        return Task.CompletedTask;
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private string GetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue))
            return headerValue.ToString();

        return Guid.NewGuid().ToString("N");
    }

    private string GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            return forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim() ?? "Unknown";

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private async Task<string?> GetRequestBodyAsync(HttpRequest request)
    {
        if (!_logRequestResponse ||
            request.ContentLength == null ||
            request.ContentLength == 0 ||
            !IsLoggableContentType(request.ContentType))
        {
            return null;
        }

        try
        {
            request.EnableBuffering();

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return TruncateIfTooLarge(body, 3000);
        }
        catch
        {
            return "[Unable to read body]";
        }
    }

    private bool IsLoggableContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Contains("application/json") ||
               contentType.Contains("application/xml") ||
               contentType.Contains("text/");
    }

    private string TruncateIfTooLarge(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text[..maxLength] + $"... [Truncated {text.Length - maxLength} chars]";
    }

    private LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }

    private string GetPerformanceCategory(long elapsedMilliseconds)
    {
        return elapsedMilliseconds switch
        {
            < 100 => "Excellent",
            < 500 => "Good",
            < 1000 => "Acceptable",
            < 3000 => "Slow",
            _ => "VerySlow"
        };
    }
}
