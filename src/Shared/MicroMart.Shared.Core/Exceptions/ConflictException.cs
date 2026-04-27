using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.Shared.Core.Exceptions;
public sealed class ConflictException(string message) : Exception(message)
{
    public static ConflictException For<T>(string reason)
        => new($"{typeof(T).Name} conflict: {reason}");
}
