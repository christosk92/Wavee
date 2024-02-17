using Eum.Spotify;
using Google.Protobuf;
using Wavee.Core.Extensions;

namespace Wavee.Spotify.Auth;

public sealed class UsernamePasswordAuthenticator : SpotifyTcpAuthenticator
{
    /// <summary>
    ///   Initiate a new instance. The token will be refreshed once it expires.
    ///   The initialToken will be updated with the new values on refresh!
    /// </summary>
    public UsernamePasswordAuthenticator(string username, string password)
    {
        Guard.NotNullOrEmptyOrWhitespace(username, nameof(username));
        Guard.NotNullOrEmptyOrWhitespace(password, nameof(password));

        Username = username;

        var initialCredentials = new LoginCredentials
        {
            Username = username,
            AuthData = ByteString.CopyFromUtf8(password),
            Typ = AuthenticationType.AuthenticationUserPass
        };
        AuthClient = new SpotifyInternalAuthClient(initialCredentials);
    }
    
    public string Username { get; }
}