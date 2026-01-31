// Extensions/SwaggerExtensions.cs
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MicroMart.ApiGateway.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MicroMart API Gateway",
                Version = "v1.0.0",
                Description = "Single entry point for all MicroMart microservices",
                Contact = new OpenApiContact
                {
                    Name = "API Gateway Team",
                    Email = "gateway@micromart.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Add XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add security definition for API Key
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key for accessing the gateway"
            });

            // Add security definition for Bearer Token (JWT)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            });

            // Add security requirements
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add custom operation filter for YARP routes
            options.OperationFilter<YarpRoutesOperationFilter>();
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroMart API Gateway v1");
            options.RoutePrefix = "api-docs"; // Changed from "swagger" to "api-docs"
            options.DocumentTitle = "MicroMart API Documentation";
            options.EnablePersistAuthorization(); // Remember authorization
            options.EnableDeepLinking(); // Enable deep linking for headings
            options.DisplayOperationId(); // Show operation IDs
            options.DefaultModelsExpandDepth(2); // Expand models to depth 2
            options.DefaultModelExpandDepth(2); // Expand model schemas
            options.DisplayRequestDuration(); // Show request duration
            options.EnableFilter(); // Enable filtering

            // Custom styling (optional)
            options.InjectStylesheet("/swagger-ui/custom.css");
            options.InjectJavascript("/swagger-ui/custom.js");
        });

        return app;
    }
}

// Custom operation filter for YARP routes
public class YarpRoutesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // You can customize operation details here if needed
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        // Add correlation ID header parameter for all operations
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Description = "Correlation ID for request tracing",
            Required = false,
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}