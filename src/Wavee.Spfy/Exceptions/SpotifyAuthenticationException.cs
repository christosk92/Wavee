using Eum.Spotify;

namespace Wavee.Spfy.Exceptions
{
    /// <summary>
    /// Please see <see cref="Failed"/> for more info.
    /// </summary>
    public class SpotifyAuthenticationException : Exception
    {
        internal SpotifyAuthenticationException(APLoginFailed failed) : base(failed.ErrorCode.ToString())
        {
            Failed = failed;
        }
        public APLoginFailed Failed { get; }
    }
}