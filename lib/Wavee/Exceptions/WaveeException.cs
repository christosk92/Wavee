namespace Wavee.Exceptions;

public abstract class WaveeException : Exception
{
    public WaveeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    
    public WaveeException(string message)
        : base(message)
    {
    }
}