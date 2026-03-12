using FluentValidation;
using MicroMart.ProductCatalog.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed class CreateProductValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator(IProductRepository repo)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .Matches(@"^[A-Z0-9\-]{2,50}$")
            .WithMessage("SKU must be uppercase alphanumeric (2-50 chars)")
            .MustAsync(async (sku, ct) => !await repo.ExistsBySkuAsync(sku, ct))
            .WithMessage(x => $"SKU '{x.Sku}' already exists");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g. USD)")
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be uppercase letters");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative");

        RuleForEach(x => x.Tags)
            .MaximumLength(50).WithMessage("Each tag cannot exceed 50 characters")
            .When(x => x.Tags != null);
    }
}
