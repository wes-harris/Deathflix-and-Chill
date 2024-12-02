using System;

namespace DeathflixAPI.Exceptions;

public class TmdbApiException : Exception
{
    public int? StatusCode { get; }

    public TmdbApiException(string message) : base(message)
    {
    }

    public TmdbApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TmdbApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}