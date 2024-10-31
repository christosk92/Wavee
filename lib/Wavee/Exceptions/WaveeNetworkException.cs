using System.Net;

namespace Wavee.Exceptions;

public class WaveeNetworkException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string ResponseContent { get; }

    public WaveeNetworkException(string message, string content, HttpStatusCode statusCode, Exception innerException)
        : base(message, innerException)
    {
        ResponseContent = content;
        StatusCode = statusCode;
    }

    public WaveeNetworkException(string message, Exception inner)
        : base(message, inner)
    {
        
    }

    public WaveeNetworkException(string message) : base(message)
    {
    }
}