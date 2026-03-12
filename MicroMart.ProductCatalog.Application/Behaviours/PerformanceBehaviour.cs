using MediatR;
using MicroMart.ProductCatalog.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Behaviours;

public sealed class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Queries should respond fast — warn above 500ms
    // Commands do more work — warn above 2000ms
    private static readonly long QueryThresholdMs = 500;
    private static readonly long CommandThresholdMs = 2000;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        var isQuery = request is ICacheableQuery;
        var threshold = isQuery ? QueryThresholdMs : CommandThresholdMs;

        if (sw.ElapsedMilliseconds > threshold)
        {
            logger.LogWarning(
                "[SLOW] {RequestType} {RequestName} took {ElapsedMs}ms — " +
                "threshold is {ThresholdMs}ms {@Request}",
                isQuery ? "Query" : "Command",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                threshold,
                request);
        }

        return response;
    }
}
