using Eum.Spotify;
using Eum.Spotify.login5v3;
using Wavee.Spotify.Core;

namespace Wavee.Spotify.Interfaces.Clients;

internal interface ISpotifyAuthenticationClient
{
    Task<LoginCredentials?> GetCredentialsFromOAuth(OAuthCallbackDelegate oAuthCallbackDelegate, CancellationToken cancellationToken);

    Task<LoginResponse> GetCredentialsFromLoginV3(LoginCredentials credentials, string deviceId, CancellationToken cancellationToken);
    
    UserInfo? UserInfo { get; }
}