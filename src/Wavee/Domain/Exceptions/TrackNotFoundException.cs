namespace Wavee.Domain.Exceptions;

public abstract class TrackNotFoundException : Exception
{
    protected TrackNotFoundException(string message, Exception innerException) : base(message, innerException)
    {

    }
}