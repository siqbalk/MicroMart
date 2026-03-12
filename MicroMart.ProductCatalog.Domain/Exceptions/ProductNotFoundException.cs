using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Exceptions;

public sealed class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(ProductId id)
        : base($"Product with id '{id.Value}' was not found") { }

    public ProductNotFoundException(string slug)
        : base($"Product with slug '{slug}' was not found") { }
}

