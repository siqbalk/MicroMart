using MediatR;
using MicroMart.ProductCatalog.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;