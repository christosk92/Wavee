using System.Text.Json;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Authentication.Requests;
using Wavee.Spotify.Common.Contracts;

namespace Wavee.Spotify.Application.Authentication.QueryHandlers;

public sealed class GetSpotifyTokenQueryHandler : IRequestHandler<GetSpotifyTokenQuery, string>
{
    private readonly ISpotifyAuthModule _spotifyAuthModule;
    private readonly IMediator _mediator;
    private readonly SpotifyClientConfig _config;

    public GetSpotifyTokenQueryHandler(ISpotifyAuthModule spotifyAuthModule, IMediator mediator,
        SpotifyClientConfig config)
    {
        _spotifyAuthModule = spotifyAuthModule;
        _mediator = mediator;
        _config = config;
    }

    public async ValueTask<string> Handle(GetSpotifyTokenQuery request, CancellationToken cancellationToken)
    {
        //TODO: Cache
        var res = await _spotifyAuthModule.GetCredentials(cancellationToken);

        var loginResponseFinal = await _mediator.Send(new SpotifyLoginV3Request
        {
            DeviceId = _config.Remote.DeviceId,
            Request = new StoredCredential
            {
                Data = ByteString.FromBase64(res.ReusableCredentialsBase64),
                Username = res.Username
            },
        }, cancellationToken);

        //Store new credentials
        await File.WriteAllTextAsync(Path.Combine(_config.Storage.Path, "credentials.json"),
            JsonSerializer.Serialize(res), cancellationToken);

        return loginResponseFinal.Ok.AccessToken;
    }
}