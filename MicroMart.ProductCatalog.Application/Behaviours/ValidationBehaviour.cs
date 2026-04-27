using FluentValidation;
using MediatR;
using MicroMart.Shared.Core.Results;


namespace MicroMart.ProductCatalog.Application.Behaviours;

public sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // No validators registered for this type — skip
        if (!validators.Any()) return await next();

        // Run all validators in parallel
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        // Collect all failures across all validators
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0) return await next();

        // Build a structured validation error from all failures
        // The first failure becomes the primary error
        // All failures are included in the message
        var errorMessages = string.Join(" | ",
            failures.Select(f => f.ErrorMessage));

        var validationError = Error.Validation(
            typeof(TRequest).Name,
            errorMessages);

        // We need to return a Result.Failure as the TResponse
        // TResponse is always Result or Result<T> for our commands/queries
        var resultType = typeof(TResponse);

        if (resultType == typeof(Result))
            return (TResponse)(object)Result.Failure(validationError);

        // Result<T> — use reflection to call Result.Failure<T>(error)
        if (resultType.IsGenericType &&
            resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = resultType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m.Name == "Failure" && m.IsGenericMethod)
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod
                .Invoke(null, new object[] { validationError })!;
        }

        // Fallback — throw ValidationException
        // Should never happen with our architecture
        throw new ValidationException(failures);
    }
}
