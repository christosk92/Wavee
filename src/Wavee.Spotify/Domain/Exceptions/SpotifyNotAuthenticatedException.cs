namespace Wavee.Spotify.Domain.Exceptions;

public sealed class SpotifyNotAuthenticatedException : Exception
{
    public SpotifyNotAuthenticatedException() : base("Spotify is not authenticated.")
    {
    }
}