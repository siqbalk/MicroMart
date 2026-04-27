using FluentValidation;
using MicroMart.Shared.Core.Exceptions;

namespace MicroMart.ProductCatalog.Api.GraphQL
{
    public class GraphQLErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.Exception switch
            {
                ValidationException ex => error
                    .WithMessage(ex.Message)
                    .WithCode("VALIDATION"),

                ConflictException ex => error
                    .WithMessage(ex.Message)
                    .WithCode("CONFLICT"),

                NotFoundException ex => error
                    .WithMessage(ex.Message)
                    .WithCode("NOT_FOUND"),

                _ => error
            };
        }
    }

    public sealed record ProductFilterInput(
    string? CategoryId,
    bool? IsActive,
    bool? IsFeatured
);

    public sealed record ProductSortInput(
        string? SortBy,
        bool Ascending = true
    );
}
