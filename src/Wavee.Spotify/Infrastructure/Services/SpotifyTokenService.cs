using Eum.Spotify;
using Google.Protobuf;
using LiteDB;
using Wavee.Spotify.Core.Models.Credentials;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class SpotifyTokenService : ISpotifyTokenService
{
    private readonly ISpotifyAuthenticationClient _authClient;
    private readonly ITcpConnectionService _tcpConnectionService;
    private readonly ISpotifyCredentialsRepository _credentialsStorage;
    private readonly WaveeSpotifyConfig _config;

    public SpotifyTokenService(ISpotifyAuthenticationClient authClient,
        ITcpConnectionService tcpConnectionService,
        ISpotifyCredentialsRepository credentialsStorage, WaveeSpotifyConfig config)
    {
        _authClient = authClient;
        _tcpConnectionService = tcpConnectionService;
        _credentialsStorage = credentialsStorage;
        _config = config;
    }

    public ValueTask<SpotifyAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var username = _tcpConnectionService.WelcomeMessage?.CanonicalUsername;

        if (!string.IsNullOrEmpty(username) &&
            _credentialsStorage.TryGetFor(username, SpotifyCredentialsType.AccessToken, out var accessToken))
        {
            if (accessToken is { IsExpired: false })
            {
                return new ValueTask<SpotifyAccessToken>(new SpotifyAccessToken
                {
                    Username = accessToken.Value.Username,
                    AccessToken =accessToken.Value.AuthDataBase64,
                    TokenType = "Bearer",
                    ExpiresAt = accessToken.Value.Expiration
                });
            }
        }

        return new ValueTask<SpotifyAccessToken>(Fetch(cancellationToken));
    }

    private async Task<SpotifyAccessToken> Fetch(CancellationToken cancellationToken)
    {
        var welcome = await _tcpConnectionService.ConnectAsync(cancellationToken);
        var storedCredentials = await _authClient.GetCredentialsFromLoginV3(new LoginCredentials
        {
            AuthData = welcome.ReusableAuthCredentials,
            Username = welcome.CanonicalUsername,
            Typ = welcome.ReusableAuthCredentialsType
        }, _config.Remote.DeviceId, cancellationToken);

        _credentialsStorage.Store(storedCredentials.Ok.Username, SpotifyCredentialsType.Full,
            new SpotifyStoredCredentialsEntity
            {
                AuthDataBase64 = welcome.ReusableAuthCredentials.ToBase64(),
                Expiration = DateTimeOffset.MaxValue,
                Id = ObjectId.NewObjectId(),
                Username = welcome.CanonicalUsername,
                Type = SpotifyCredentialsType.Full,
                InstanceId = Constants.InstanceId
            });

        
        _credentialsStorage.Store(storedCredentials.Ok.Username, SpotifyCredentialsType.AccessToken,
            new SpotifyStoredCredentialsEntity
            {
                AuthDataBase64 = storedCredentials.Ok.AccessToken,
                Expiration = DateTimeOffset.UtcNow.AddHours(1),
                Id = ObjectId.NewObjectId(),
                Username = storedCredentials.Ok.Username,
                Type = SpotifyCredentialsType.AccessToken,
                InstanceId = Constants.InstanceId
            });

        return new SpotifyAccessToken
        {
            Username = storedCredentials.Ok.Username,
            AccessToken = storedCredentials.Ok.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(storedCredentials.Ok.AccessTokenExpiresIn)
        };
    }
}