using MediatR;
using MicroMart.ProductCatalog.Domain.Primitives;

namespace MicroMart.ProductCatalog.Application.Abstractions;


// ── ICommand.cs ───────────────────────────────────────────────────────
// A command changes state — creates, updates, deletes
// Returns Result<TResponse> — can succeed or fail with an Error
// Examples: CreateProductCommand, ChangeProductPriceCommand
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

// Commands that return nothing (e.g. delete)
public interface ICommand : IRequest<Result>;

