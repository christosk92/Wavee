using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Common.Contracts;

namespace Wavee.Spotify;

internal sealed class SpotifyClient : ISpotifyClient
{
    private readonly ISpotifyRemoteClient _spotifyRemoteHolder;

    public SpotifyClient(ISpotifyRemoteClient spotifyRemoteHolder)
    {
        _spotifyRemoteHolder = spotifyRemoteHolder;
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await (_spotifyRemoteHolder as SpotifyRemoteHolder)!.Initialize(cancellationToken);
    }
}