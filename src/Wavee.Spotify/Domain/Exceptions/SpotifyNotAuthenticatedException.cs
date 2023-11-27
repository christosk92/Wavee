namespace Wavee.Spotify.Domain.Exceptions;

public sealed class SpotifyNotAuthenticatedException : Exception
{
    public SpotifyNotAuthenticatedException(string? msg = null) : base(
        msg ??
        "Spotify is not authenticated.")
    {
    }
}