namespace Wavee.Spotify.Remote;

public sealed class NoActiveDeviceException : Exception
{
    const string message = "No active device found.";

    public NoActiveDeviceException() : base(message)
    {
    }
}