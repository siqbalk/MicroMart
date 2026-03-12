using FluentValidation;
using MicroMart.ProductCatalog.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Products.Commands.CreateProduct;

public sealed class UpdateProductValidator
    : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0);
    }
}

