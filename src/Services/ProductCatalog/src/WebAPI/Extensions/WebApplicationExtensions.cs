
using MicroMart.ProductCatalog.Api.Endpoints.Categories;
using MicroMart.ProductCatalog.Api.Endpoints.Products;
using MicroMart.ProductCatalog.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // ❶ Global error handler — outermost, catches everything
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // ❷ HTTPS redirect in production
        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        // ❸ Swagger — dev + staging only
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.UseSwagger(); // still serves JSON
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog v1");
                c.RoutePrefix = "swagger"; // Access at http://localhost:65006/swagger
            });

            // Redoc UI (clean documentation)
            app.UseReDoc(c =>
            {
                c.SpecUrl("/swagger/v1/swagger.json"); // point to same OpenAPI JSON
                c.RoutePrefix = "openapi";            // Access at http://localhost:65006/openapi
                c.DocumentTitle = "Product Catalog OpenAPI UI";
            });
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog v1");
            //    c.RoutePrefix = "swagger";
            //});
        }

        // ❹ CORS
        app.UseCors("Default");

        // ❺ Auth (add when JWT is wired)
        // app.UseAuthentication();
        // app.UseAuthorization();

        // ❻ Health checks — liveness + readiness
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthJson });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = h => h.Tags.Contains("db") || h.Tags.Contains("cache"),
            ResponseWriter = WriteHealthJson
        });

        // ❼ GraphQL endpoint — /graphql
        app.MapGraphQL("/graphql");

        // ❽ Minimal API endpoint groups
        app.MapProductEndpoints();
        app.MapCategoryEndpoints();

        return app;
    }

    private static async Task WriteHealthJson(HttpContext ctx, HealthReport r)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = r.Status.ToString(),
            results = r.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), durationMs = e.Value.Duration.TotalMilliseconds })
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

// ── Middleware pipeline order reference ──────────────────────────────────────
//
//  ❶ GlobalExceptionMiddleware    catches all unhandled exceptions below
//  ❷ HttpsRedirection             enforces HTTPS in production
//  ❸ Swagger UI                   /swagger  (dev+staging only)
//  ❹ CORS                         Access-Control-* headers
//  ❺ Authentication / Authorization
//  ❻ Health checks                /health  /health/live  /health/ready
//  ❼ GraphQL                      /graphql  (Hot Chocolate)
//  ❽ Minimal API Endpoints        /api/v1/products/**  /api/v1/categories/**