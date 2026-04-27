namespace MicroMart.Shared.Core.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None
        = new(string.Empty, string.Empty);

    public static readonly Error NullValue
        = new("Error.NullValue", "Null value provided");

    // Factory methods for common errors
    public static Error NotFound(string name, object key)
        => new($"{name}.NotFound", $"{name} with id '{key}' was not found");

    public static Error Validation(string name, string message)
        => new($"{name}.Validation", message);

    public static Error Conflict(string name, string message)
        => new($"{name}.Conflict", message);
}

