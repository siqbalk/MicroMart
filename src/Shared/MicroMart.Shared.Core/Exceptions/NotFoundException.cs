using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.Shared.Core.Exceptions;
public sealed class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For<T>(object key)
        => new($"{typeof(T).Name} with id '{key}' was not found.");
}
