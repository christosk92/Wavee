using Eum.Spotify;

namespace Wavee.Spotify.Domain.Exceptions;

public sealed class SpotifyLegacyAuthenticationException : Exception
{
    public SpotifyLegacyAuthenticationException(APLoginFailed loginFailed)
        : base("Failed to authenticate with Spotify.")
    {
        LoginFailed = loginFailed;
    }

    public APLoginFailed LoginFailed { get; }
}