using System;
using System.Collections.Generic;
using System.Text;

namespace MicroMart.Shared.Core.Exceptions;
public sealed class UnauthorizedException(string message = "Unauthorized.")
    : Exception(message);
