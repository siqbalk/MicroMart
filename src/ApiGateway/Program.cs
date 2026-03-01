// Program.cs - Complete working version
using AspNetCoreRateLimit;
using MicroMart.ApiGateway.ExceptionHandlers;
using MicroMart.ApiGateway.Extensions;
using MicroMart.ApiGateway.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.AddConfigurationFiles();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();


builder.Host.UseSerilog();

try
{
    Log.Information("Starting MicroMart API Gateway...");

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 🔧 CRITICAL: Add Swagger with explicit configuration
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MicroMart API Gateway",
            Version = "v1.0.0",
            Description = "Gateway for MicroMart microservices"
        });
    });

    // Add Gateway Services
    builder.Services
        .AddGatewayConfiguration(builder.Configuration)
        .AddReverseProxy(builder.Configuration)
        .AddSecurityServices(builder.Configuration)
        .AddHealthChecks(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    if (builder.Environment.IsProduction())
    {
        // In production (Azure), listen on 0.0.0.0:8080
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
        });
    }
    else
    {
        // In development, use the settings from appsettings.json
        builder.WebHost.UseKestrel();
    }

    var app = builder.Build();


    app.UseExceptionHandler();

    // Static files (for swagger UI)
    app.UseStaticFiles();

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroMart API v1");
            c.RoutePrefix = "swagger";
        });

        app.UseDeveloperExceptionPage();
        app.UseHttpsRedirection();
    }

    // Custom middlewares
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseForwardedHeaders();

    app.UseIpRateLimiting();

    //app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("GatewayCorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOutputCache();


    // ======================
    // ENDPOINT MAPPING
    // ======================


    // Health
    app.MapHealthChecks("/health");

    // Controllers
    app.MapControllers();

    // 🔥 Reverse proxy (ONLY ONCE and LAST)
    app.MapReverseProxy()
    .RequireAuthorization();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}