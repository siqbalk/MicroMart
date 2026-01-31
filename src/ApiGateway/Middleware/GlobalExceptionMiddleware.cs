// ExceptionHandlers/GlobalExceptionHandler.cs
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MicroMart.ApiGateway.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"] as string
            ?? Guid.NewGuid().ToString("N");

        _logger.LogError(exception,
            "Global Exception Handler: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId, httpContext.Request.Path, httpContext.Request.Method);

        var problemDetails = new ProblemDetails
        {
            Status = GetStatusCode(exception),
            Type = GetProblemType(exception),
            Title = GetTitle(exception),
            Detail = _environment.IsDevelopment() ? exception.Message : null,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Extensions = { ["correlationId"] = correlationId }
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["innerException"] = exception.InnerException?.Message;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetProblemType(Exception exception)
    {
        return $"https://httpstatuses.com/{GetStatusCode(exception)}";
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            KeyNotFoundException => "Not Found",
            NotImplementedException => "Not Implemented",
            TimeoutException => "Request Timeout",
            _ => "Internal Server Error"
        };
    }
}