using FluentValidation;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;

namespace MicroMart.ProductCatalog.Application.Products.Commands;

public sealed record UpdateProductCommand(
    string ProductId,
    string Name,
    string Description,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    Dictionary<string, string>? Attributes
) : ICommand<ProductResponse>;
