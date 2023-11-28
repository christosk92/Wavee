using Eum.Spotify;
using Eum.Spotify.connectstate;
using Mediator;
using NeoSmart.AsyncLock;
using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.AudioKeys;
using Wavee.Spotify.Application.Library;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Application.Remote.Queries;
using Wavee.Spotify.Application.StorageResolve;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.State;
using Wavee.Spotify.Domain.Tracks;
using Wavee.Spotify.Domain.User;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify;

internal sealed class SpotifyClient : ISpotifyClient
{
    private AsyncLock _playbackStateLock = new();
    private SpotifyPlaybackState _playbackState;
    private readonly SpotifyRemoteHolder _spotifyRemoteHolder;
    private readonly SpotifyTcpHolder _tcpHolder;
    
    private readonly IMediator _mediator;

    public SpotifyClient(SpotifyRemoteHolder spotifyRemoteHolder,
        IMediator mediator,
        IWaveePlayer player,
        SpotifyClientConfig config,
        ISpotifyTrackClient tracks, 
        ISpotifyAudioKeyClient audioKeys, 
        ISpotifyStorageResolver storageResolver,
        SpotifyTcpHolder tcpHolder, ISpotifyLibraryClient library)
    {
        _spotifyRemoteHolder = spotifyRemoteHolder;
        _mediator = mediator;
        Player = player;
        Config = config;
        Tracks = tracks;
        AudioKeys = audioKeys;
        StorageResolver = storageResolver;
        _tcpHolder = tcpHolder;
        Library = library;
        _playbackState = SpotifyPlaybackState.InActive();
        _spotifyRemoteHolder.RemoteStateChanged += OnRemoteStateChanged;
    }

    public event EventHandler<SpotifyPlaybackState> PlaybackStateChanged;

    public SpotifyClientConfig Config { get; }

    public IWaveePlayer Player { get; }
    public ISpotifyTrackClient Tracks { get; }
    public ISpotifyAudioKeyClient AudioKeys { get; }
    public ISpotifyStorageResolver StorageResolver { get; }
    public ISpotifyLibraryClient Library { get; }
    public Task<APWelcome> User => _tcpHolder.WelcomeMessage;

    public async Task<Me> Initialize(CancellationToken cancellationToken = default)
    {
        await _spotifyRemoteHolder.Initialize(cancellationToken);
        return new Me();
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