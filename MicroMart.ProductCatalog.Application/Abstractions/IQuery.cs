using MediatR;
using MicroMart.Shared.Core.Results;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;