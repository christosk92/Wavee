namespace Wavee.Spotify.Domain.Exceptions;

public sealed class SpotifyNoStoredCredentialsException : Exception
{
    public SpotifyNoStoredCredentialsException() : base("No stored credentials found.")
    {
    }
}