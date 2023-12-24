using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyEpisodeClient : ISpotifyEpisodeClient
{
    private readonly ISpotifyTokenService _tokenService;
    private readonly SpotifyInternalHttpClient _httpClient;
    public SpotifyEpisodeClient(ISpotifyTokenService tokenService, SpotifyInternalHttpClient httpClient)
    {
        _tokenService = tokenService;
        _httpClient = httpClient;
    }
}