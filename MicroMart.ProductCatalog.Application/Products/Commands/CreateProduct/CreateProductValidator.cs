using FluentValidation;
using MicroMart.ProductCatalog.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000);

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100)
            .Matches(@"^[A-Z0-9\-_]+").WithMessage("SKU must contain only uppercase letters, digits, hyphens, or underscores.");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.PriceCurrency)
            .NotEmpty()
            .Length(3).WithMessage("Currency must be a 3-letter ISO 4217 code.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LengthCm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WidthCm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.HeightCm).GreaterThanOrEqualTo(0);
    }
}
