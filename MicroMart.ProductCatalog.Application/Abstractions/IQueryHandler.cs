using MediatR;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
