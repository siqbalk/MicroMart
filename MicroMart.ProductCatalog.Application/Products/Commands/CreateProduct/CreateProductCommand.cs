using FluentValidation;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal PriceAmount,
    string PriceCurrency,
    int InitialStock,
    string CategoryId,
    decimal WeightKg = 0,
    decimal LengthCm = 0,
    decimal WidthCm = 0,
    decimal HeightCm = 0,
    Dictionary<string, string>? Attributes = null)
    : ICommand<ProductResponse>;



