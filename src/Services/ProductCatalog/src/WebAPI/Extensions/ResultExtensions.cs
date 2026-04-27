using MicroMart.Shared.Core.Results;
using Error = MicroMart.Shared.Core.Results.Error;  // ← Result<T>, Result, Error

namespace MicroMart.ProductCatalog.Api.Extensions;

/// Converts Application layer Result<T> into ASP.NET Core TypedResults (IResult)
/// Used in every Minimal API endpoint handler.
public static class ResultExtensions
{
    // Result<T> with a custom success factory
    public static IResult ToApiResult<T>(
        this Shared.Core.Results.Result<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess?.Invoke(result.Value) ?? TypedResults.Ok(result.Value);

        return result.Error.Code switch
        {
            "NotFound" => TypedResults.NotFound(Problem(result.Error)),
            "Conflict" => TypedResults.Conflict(Problem(result.Error)),
            "Forbidden" => TypedResults.Forbid(),
            _ => TypedResults.BadRequest(Problem(result.Error))
        };
    }

    // Result (no value) — default success = 204 NoContent
    public static IResult ToApiResult(
        this Result result,
        Func<IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess?.Invoke() ?? TypedResults.NoContent();

        return result.Error.Code switch
        {
            "NotFound" => TypedResults.NotFound(Problem(result.Error)),
            "Conflict" => TypedResults.Conflict(Problem(result.Error)),
            _ => TypedResults.BadRequest(Problem(result.Error))
        };
    }

    private static object Problem(Error e) => new
    {
        title = e.Code,
        detail = e.Message,
        type = $"https://micromart.com/errors/{e.Code.ToLowerInvariant()}"
    };
}