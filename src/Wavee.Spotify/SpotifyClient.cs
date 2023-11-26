using Eum.Spotify.connectstate;
using Mediator;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Application.Remote.Queries;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.State;

namespace Wavee.Spotify;

internal sealed class SpotifyClient : ISpotifyClient
{
    private AsyncLock _playbackStateLock = new();
    private SpotifyPlaybackState _playbackState;
    private readonly SpotifyRemoteHolder _spotifyRemoteHolder;
    private readonly IMediator _mediator;

    public SpotifyClient(SpotifyRemoteHolder spotifyRemoteHolder, IMediator mediator)
    {
        _spotifyRemoteHolder = spotifyRemoteHolder;
        _mediator = mediator;
        _playbackState = SpotifyPlaybackState.InActive();
        _spotifyRemoteHolder.RemoteStateChanged += OnRemoteStateChanged;
    }

    public event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;
    
    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await _spotifyRemoteHolder.Initialize(cancellationToken);
    }
    
    private async void OnRemoteStateChanged(object? sender, Cluster e)
    {
        using (await _playbackStateLock.LockAsync())
        {
            var state = await _mediator.Send(new ClusterToPlaybackStateQuery
            {
                Cluster = e
            });
            
            _playbackState = state;
            PlaybackStateChanged?.Invoke(this, state);
        }
    }
}