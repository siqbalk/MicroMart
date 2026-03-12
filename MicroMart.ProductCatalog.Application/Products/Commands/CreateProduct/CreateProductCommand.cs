using FluentValidation;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    int InitialStock,
    string CategoryId,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    Dictionary<string, string>? Attributes,
    List<string>? Tags
) : ICommand<ProductResponse>;



