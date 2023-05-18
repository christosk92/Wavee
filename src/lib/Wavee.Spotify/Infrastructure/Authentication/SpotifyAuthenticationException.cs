using Eum.Spotify;

namespace Wavee.Spotify.Infrastructure.Authentication;

public sealed class SpotifyAuthenticationException : Exception
{
    public SpotifyAuthenticationException(APLoginFailed authFailure)
        : base(authFailure.ErrorCode.ToString())
    {
        AuthFailure = authFailure;
    }

    public APLoginFailed AuthFailure { get; }
}