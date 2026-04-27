// MicroMart.Shared.Core/Results/ResultExtensions.cs

namespace MicroMart.Shared.Core.Results;

public static class ResultExtensions
{
    // Used in GraphQL resolvers — unwraps value or throws
    // Hot Chocolate catches the exception and puts it in errors[]
    public static T ThrowIfError<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value;

        throw result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase)
                => new KeyNotFoundException(result.Error.Message),

            var c when c.EndsWith(".Conflict", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException(result.Error.Message),

            var c when c.EndsWith(".Validation", StringComparison.OrdinalIgnoreCase)
                => new ArgumentException(result.Error.Message),

            var c when c.EndsWith(".Forbidden", StringComparison.OrdinalIgnoreCase)
                => new UnauthorizedAccessException(result.Error.Message),

            _ => new InvalidOperationException(result.Error.Message)
        };
    }

    // Overload for Result (no value) — used in void mutations
    public static void ThrowIfError(this Result result)
    {
        if (result.IsSuccess) return;

        throw result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase)
                => new KeyNotFoundException(result.Error.Message),

            var c when c.EndsWith(".Conflict", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException(result.Error.Message),

            var c when c.EndsWith(".Validation", StringComparison.OrdinalIgnoreCase)
                => new ArgumentException(result.Error.Message),

            _ => new InvalidOperationException(result.Error.Message)
        };
    }
}