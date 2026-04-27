using FluentValidation;
using MicroMart.Shared.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions _opts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteAsync(ctx, ex);
        }
    }

    private static async Task WriteAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/problem+json";

        ProblemDetails problem = ex switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors.GroupBy(e => e.PropertyName)
                         .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            { Status = 400, Title = "Validation failed", Instance = ctx.Request.Path },

            NotFoundException nfe => new() { Status = 404, Title = "Not found", Detail = nfe.Message, Instance = ctx.Request.Path },
            ConflictException ce => new() { Status = 409, Title = "Conflict", Detail = ce.Message, Instance = ctx.Request.Path },
            UnauthorizedException => new() { Status = 401, Title = "Unauthorized" },
            _ => new() { Status = 500, Title = "Server error", Detail = "An unexpected error occurred." }
        };

        ctx.Response.StatusCode = problem.Status ?? 500;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, problem.GetType(), _opts));
    }
}