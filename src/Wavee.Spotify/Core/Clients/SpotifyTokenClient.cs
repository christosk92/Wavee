using Wavee.Spotify.Core.Models.Credentials;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyTokenClient : ISpotifyTokenClient
{
    private readonly ISpotifyTokenService _tokenService;

    public SpotifyTokenClient(ISpotifyTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public ValueTask<SpotifyAccessToken> GetAccessToken(CancellationToken cancellationToken = default) => _tokenService.GetAccessTokenAsync(cancellationToken);
}