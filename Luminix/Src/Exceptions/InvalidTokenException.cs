using System;

namespace Luminix.Exceptions;

public class InvalidTokenException : Exception
{
    public InvalidTokenException()
        : base("The provided token is invalid, has already been used, or was not issued by the registry.")
    {
    }

    public InvalidTokenException(string message)
        : base(message)
    {
    }

    public InvalidTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}