using Eum.Spotify;

namespace Wavee.Spotify.Infrastructure.Connection;

public sealed class SpotifyAuthenticationException : Exception
{
    internal SpotifyAuthenticationException(APLoginFailed failed) : base(
        failed.ErrorCode.ToString())
    {
        ErrorCode = failed;
    }

    public APLoginFailed ErrorCode { get; }
}