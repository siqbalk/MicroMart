using MediatR;
using MicroMart.ProductCatalog.Domain.Primitives;

namespace MicroMart.ProductCatalog.Application.Abstractions;

public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;
