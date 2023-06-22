namespace Wavee.Spotify.Infrastructure.Connection;

public sealed class NotConnectedException : Exception
{
    public NotConnectedException(string message) : base(message)
    {
    }
}