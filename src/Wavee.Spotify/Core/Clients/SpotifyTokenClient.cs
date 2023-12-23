using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Interfaces.Clients;
using Wavee.Spotify.Core.Models.Credentials;

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