using MicroMart.ProductCatalog.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Exceptions;

public sealed class CategoryNotFoundException : DomainException
{
    public CategoryNotFoundException(CategoryId id)
        : base($"Category with id '{id.Value}' was not found") { }
}
