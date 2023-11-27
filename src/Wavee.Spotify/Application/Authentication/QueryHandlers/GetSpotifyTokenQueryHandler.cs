using Eum.Spotify.login5v3;
using Google.Protobuf;
using Mediator;
using Nito.AsyncEx;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Authentication.Requests;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify.Application.Authentication.QueryHandlers;

public sealed class GetSpotifyTokenQueryHandler : IRequestHandler<GetSpotifyTokenQuery, string>
{
    private static readonly AsyncLock _lock = new();

    private readonly ISpotifyAccessTokenRepository _spotifyAccessTokenRepository;
    private readonly ISpotifyAuthModule _spotifyAuthModule;
    private readonly IMediator _mediator;
    private readonly SpotifyClientConfig _config;
    private readonly ISpotifyStoredCredentialsRepository _spotifyStoredCredentialsRepository;

    public GetSpotifyTokenQueryHandler(
        ISpotifyAuthModule spotifyAuthModule,
        IMediator mediator,
        SpotifyClientConfig config,
        ISpotifyAccessTokenRepository spotifyAccessTokenRepository,
        ISpotifyStoredCredentialsRepository spotifyStoredCredentialsRepository)
    {
        _spotifyAuthModule = spotifyAuthModule;
        _mediator = mediator;
        _config = config;
        _spotifyAccessTokenRepository = spotifyAccessTokenRepository;
        _spotifyStoredCredentialsRepository = spotifyStoredCredentialsRepository;
    }

    public async ValueTask<string> Handle(GetSpotifyTokenQuery request, CancellationToken cancellationToken)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            string? usernameToCheck = request.Username;
            if (string.IsNullOrEmpty(usernameToCheck))
            {
                var defaultUser = await _spotifyStoredCredentialsRepository.GetDefaultUser(cancellationToken);
                usernameToCheck = defaultUser;
            }

            if (!string.IsNullOrEmpty(usernameToCheck))
            {
                var accessToken = 
                    await _spotifyAccessTokenRepository.GetAccessToken(usernameToCheck, cancellationToken);
                if (accessToken is { IsExpired: false })
                {
                    return accessToken.Value.Value;
                }

                if (accessToken is { IsExpired: true, RefreshToken: { } refreshToken })
                {
                    //TODO: Refresh
                    //For now, just get new token.
                }
            }

            // Fetch credentials
            var res = await _spotifyAuthModule.GetCredentials(usernameToCheck, cancellationToken);

            await _spotifyStoredCredentialsRepository.StoreCredentials(
                credentials: res,
                isDefault: _spotifyAuthModule.IsDefault,
                cancellationToken: cancellationToken);

            var loginResponseFinal = await _mediator.Send(new SpotifyLoginV3Request
            {
                DeviceId = _config.Remote.DeviceId,
                Request = new StoredCredential
                {
                    Data = ByteString.FromBase64(res.ReusableCredentialsBase64),
                    Username = res.Username
                },
            }, cancellationToken);

            //Store
            await _spotifyAccessTokenRepository.StoreAccessToken(loginResponseFinal.Ok, cancellationToken);

            return loginResponseFinal.Ok.AccessToken;
        }
    }
}