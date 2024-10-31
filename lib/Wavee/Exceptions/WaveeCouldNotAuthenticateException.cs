using System.Net;

namespace Wavee.Exceptions;

public class WaveeCouldNotAuthenticateException : WaveeNetworkException
{
    public WaveeCouldNotAuthenticateException(string message, string content, HttpStatusCode statusCode, Exception innerException)
        : base(message, content, statusCode, innerException)
    {
    }
    
    public WaveeCouldNotAuthenticateException(string message)
        : base(message)
    {
    }
}