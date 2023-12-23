using Eum.Spotify;
using Wavee.Spotify.Core;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Infrastructure.HttpClients;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class AuthenticationService : IAuthenticationService
{
    private readonly ISpotifyCredentialsRepository _credentialsStorage;
    private readonly OAuthCallbackDelegate _oAuthCallbackDelegate;
    private readonly WaveeSpotifyConfig _config;
    private readonly ISpotifyAuthenticationClient _authenticationClient;
    public AuthenticationService(OAuthCallbackDelegate oAuthCallbackDelegate,
        ISpotifyCredentialsRepository credentialsStorage,
        WaveeSpotifyConfig config, 
        ISpotifyAuthenticationClient authenticationClient)
    {
        _oAuthCallbackDelegate = oAuthCallbackDelegate;
        _credentialsStorage = credentialsStorage;
        _config = config;
        _authenticationClient = authenticationClient;
    }

    public async Task<(LoginCredentials credentials, string deviceId)> GetCredentials(CancellationToken cancellationToken = default)
    {
        var deviceId = _config.Remote.DeviceId;
        
        // try to get credentials from storage
        if (_credentialsStorage.TryGetDefault(SpotifyCredentialsType.OAuth, out var credentials))
        {
            return (credentials.ToLoginCredentials()!, deviceId);
        }
        
        // get credentials from OAuth
        var storedCredentials = await _authenticationClient.GetCredentialsFromOAuth(_oAuthCallbackDelegate, cancellationToken);
        // Store them as default
        _credentialsStorage.Store(storedCredentials!.Username, SpotifyCredentialsType.OAuth, storedCredentials.ToStoredCredentials());
        return (storedCredentials, deviceId);
    }

}