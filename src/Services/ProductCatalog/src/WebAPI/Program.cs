using MicroMart.ProductCatalog.Api.Extensions;
using MicroMart.ProductCatalog.Application;
using MicroMart.ProductCatalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiMiddleware();

app.Run();