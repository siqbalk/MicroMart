using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Domain.Interfaces;

public interface IUnitOfWork
{
   Task  SaveChangesAsync(CancellationToken ct);
}
