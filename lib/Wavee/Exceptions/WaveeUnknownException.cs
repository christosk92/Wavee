namespace Wavee.Exceptions;

public class WaveeUnknownException : WaveeException
{
    public WaveeUnknownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    
    public WaveeUnknownException(string message)
        : base(message)
    {
    }
}