using Wavee.Spotify.Authenticators;

namespace Wavee.Spotify.Auth.OAuth
{
    public sealed class OAuthAuthenticator : SpotifyTcpAuthenticator
    {
        public OAuthAuthenticator(OpenBrowserRequest openBrowserRequestDelegate)
        {
            AuthClient = new SpotifyInternalAuthClient(openBrowserRequestDelegate);
        }
    }
}