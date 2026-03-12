using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Exceptions;

public sealed class DuplicateSkuException : DomainException
{
    public string Sku { get; }
    public DuplicateSkuException(string sku)
        : base($"A product with SKU '{sku}' already exists")
        => Sku = sku;
}