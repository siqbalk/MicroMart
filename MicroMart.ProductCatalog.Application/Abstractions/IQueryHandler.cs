using MediatR;
using MicroMart.ProductCatalog.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
