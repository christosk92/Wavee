using Eum.Spotify;

namespace Wavee.Spotify.Exceptions;

public sealed class SpotifyAuthenticationException : Exception
{
    internal SpotifyAuthenticationException(APLoginFailed failed) : base(
        failed.ErrorCode.ToString())
    {
        ErrorCode = failed;
    }

    public APLoginFailed ErrorCode { get; }
}