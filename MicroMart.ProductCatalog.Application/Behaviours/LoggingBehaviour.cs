using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Behaviours;

public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "[START] {RequestName} {@Request}",
            requestName, request);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation(
                "[END] {RequestName} — completed in {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "[ERROR] {RequestName} failed after {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw; // rethrow — global exception handler catches it
        }
    }
}