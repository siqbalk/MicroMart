using FluentValidation;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Application.DTOs;
using MicroMart.ProductCatalog.Domain.Interfaces;

namespace MicroMart.ProductCatalog.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(string Id, string Name, string Description)
    : ICommand<ProductResponse>;