using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var products = new List<Product>
{
    new Product(1, "Laptop", 1200, "Gaming laptop"),
    new Product(2, "Mouse", 25, "Wireless mouse"),
    new Product(3, "Keyboard", 75, "Mechanical keyboard"),
    new Product(4, "Monitor", 300, "27 inch display")
};

app.MapGet("/api/products", () =>
{
    return Results.Ok(products);
});



app.Run();

public record Product(int Id, string Name, decimal Price, string? Description);
