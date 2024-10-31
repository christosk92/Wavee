using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eum.Spotify.connectstate;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using AsyncLock = NeoSmart.AsyncLock.AsyncLock;

namespace Wavee.Services.Playback.Remote;

internal sealed class SpotifyWebsocketState : ISpotifyWebsocketState
{
    private readonly ILogger<SpotifyWebsocketState> _logger;
    private readonly ISpotifyApiClient _api;
    private readonly IWebsocketFactory _websocketFactory;
    private readonly SpotifyWebsocketRouter _router = new();

    private readonly SpotifyConfig _config;
    private readonly CancellationTokenSource _masterCts = new();
    private readonly AsyncManualResetEvent _connectedEvent = new(false);
    private readonly AsyncLock _lock = new();
    private IDisposable? _subscription;

    private ISpotifyWebsocket? _socket;
    private readonly BehaviorSubject<SpotifyRemotePlaybackState?> _playbackStateSubj;
    private readonly BehaviorSubject<Unit> _reconnectedSubj = new BehaviorSubject<Unit>(Unit.Default);
    private readonly BehaviorSubject<bool> _connectedSubj = new BehaviorSubject<bool>(false);

    private PutStateRequest? _putStateRequest;

    private readonly Dictionary<string, TaskCompletionSource<Unit>> _acknowledgements = new();
    private readonly List<string> _earlyAcknowledgements = [];

    public SpotifyWebsocketState(
        SpotifyConfig config,
        ISpotifyApiClient api,
        IWebsocketFactory websocketFactory,
        ILogger<SpotifyWebsocketState> logger, IWaveePlayer player)
    {
        _config = config;
        _api = api;
        _websocketFactory = websocketFactory;
        _logger = logger;
        _player = player;
        _playbackStateSubj = new BehaviorSubject<SpotifyRemotePlaybackState?>(null);

        Task.Run(Runner);
        RegisterMessageHandlers();
    }

    public IDisposable AddMessageHandler(string path, SpotifyWebsocketRouter.MessageHandler handler)
    {
        _router.AddMessageHandler(path, handler);
        var remove = Disposable.Create(() => _router.RemoveMessageHandler(handler));
        return remove;
    }

    public void AddRequestHandler(string path, SpotifyWebsocketRouter.RequestHandler handler)
    {
        _router.AddRequestHandler(path, handler);
    }

    public IObservable<SpotifyRemotePlaybackState?> PlaybackState => _playbackStateSubj;
    public string? ConnectionId => _socket?.ConnectionId;
    public IObservable<Unit> Reconnected => _reconnectedSubj.Skip(1);
    public IObservable<bool> Connected => _connectedSubj.DistinctUntilChanged();

    private void RegisterMessageHandlers()
    {
        _router.AddMessageHandler("hm://connect-state/v1/cluster", HandleClusterMessage);
    }

    private async Task Runner()
    {
        bool attemptReconnect = false;
        while (!_masterCts.Token.IsCancellationRequested)
        {
            if (!attemptReconnect)
            {
                await _connectedEvent.WaitAsync(_masterCts.Token);
            }

            try
            {
                await EnsureConnectedAsync(_masterCts.Token);
                var completedTcs = new TaskCompletionSource<bool>();
                var sub1 =
                    _socket!.Messages
                        .Where(x => x.Type is SpotifyWebsocketMessageType.Message)
                        .Select(message => Observable.FromAsync(ct => _router.RouteMessageAsync(message, ct)))
                        .Merge()
                        .Subscribe(
                            _ => { },
                            ex => { _logger.LogError(ex, "Error routing message"); },
                            () => { completedTcs.SetResult(true); });


                var sub2 =
                    _socket!.Messages
                        .Where(x => x.Type is SpotifyWebsocketMessageType.Request)
                        .Select(message =>
                            Observable.FromAsync(ct => _router.RouteRequestAsync(message, _socket!.Reply, ct)))
                        .Merge()
                        .Subscribe(
                            _ => { },
                            ex =>
                            {
                                _logger.LogError(ex, "Error routing message");
                                completedTcs.TrySetResult(true);
                            },
                            () => { completedTcs.TrySetResult(true); });

                var sub3 = _socket.Disposed.Subscribe(_ =>
                {
                    _logger.LogInformation("Websocket disposed");
                    completedTcs.TrySetResult(true);
                });
                _subscription = new CompositeDisposable(sub1, sub2, sub3);

                if (attemptReconnect)
                {
                    _reconnectedSubj.OnNext(Unit.Default);
                    attemptReconnect = false;
                }

                await completedTcs.Task;
            }
            catch (OperationCanceledException x)
            {
                if (x.CancellationToken == _masterCts.Token)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An error occurred in the session runner.. We will attempt a reconnect in 5 seconds");
            }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                _subscription?.Dispose();
                _connectedEvent.Reset();
                _subscription?.Dispose();

                attemptReconnect = true;
                _connectedSubj.OnNext(false);
                await Task.Delay(5000, _masterCts.Token);
            }
        }

        _masterCts.Dispose();
    }

    private async Task HandleClusterMessage(SpotifyWebsocketMessage message,
        IDictionary<string, string> parameters,
        CancellationToken cancellationtoken)
    {
        using (await _acknowledgementsLock.LockAsync(cancellationtoken))
        {
            var clusterUpdate = ClusterUpdate.Parser.ParseFrom(message.Payload);
            if (!string.IsNullOrEmpty(clusterUpdate.AckId))
            {
                if (_acknowledgements.TryGetValue(clusterUpdate.AckId, out var tcs))
                {
                    tcs.SetResult(Unit.Default);
                    _acknowledgements.Remove(clusterUpdate.AckId);
                }
                else
                {
                    _earlyAcknowledgements.Add(clusterUpdate.AckId);
                }
            }

            await NewCluster(clusterUpdate.Cluster, clusterUpdate.AckId);
        }
    }

    private async Task<Device?> EnsureConnectedAsync(CancellationToken token)
    {
        using (await _lock.LockAsync(token))
        {
            if (_socket is not null && _socket.Connected)
            {
                _connectedSubj.OnNext(true);
                return null;
            }

            _socket = await _websocketFactory.CreateWebsocket(token);

            // Announce to Spotify API that we are connected
            var state = await AnnounceDevice(token);

            _connectedEvent.Set();
            _connectedSubj.OnNext(true);
            return state;
        }
    }

    public ValueTask<Device?> ConnectAsync(string deviceName, DeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        using (_lock.Lock(cancellationToken))
        {
            // disconnect if we are already connected
            if (_socket is not null)
            {
                _socket.Dispose();
                _socket = null;
            }


            _config.Playback.DeviceName = deviceName;
            _config.Playback.DeviceType = deviceType;
            return new ValueTask<Device?>(EnsureConnectedAsync(cancellationToken));
        }
    }

    public async Task ForceNewClusterUpdate(Cluster newRemoteState)
    {
        await NewCluster(newRemoteState, null);
    }

    private async Task<Device> AnnounceDevice(CancellationToken token)
    {
        if (_putStateRequest is null)
        {
            _putStateRequest = new PutStateRequest
            {
                PutStateReason = PutStateReason.NewDevice,
                Device = NewState(),
                MemberType = MemberType.ConnectState
            };
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            _putStateRequest.ClientSideTimestamp = (ulong)now.ToUnixTimeMilliseconds();
            _putStateRequest.Device.PlayerState.Timestamp = (long)now.ToUnixTimeMilliseconds();
            _putStateRequest.Device.PlayerState.PositionAsOfTimestamp = (long)_player.Position.TotalMilliseconds;
        }

        var remoteState =
            await _api.PutConnectState(_config.Playback.DeviceId, _socket!.ConnectionId, _putStateRequest, token);
        await NewCluster(remoteState, null);
        return _putStateRequest.Device;
    }

    private readonly IDictionary<SpotifyId, SpotifyPlayableItem?> _trackCache =
        new Dictionary<SpotifyId, SpotifyPlayableItem?>();

    private readonly AsyncLock _tracksCacheLock = new();
    private PlayerState? _playerState = new PlayerState();
    private readonly IWaveePlayer _player;
    private readonly AsyncLock _acknowledgementsLock = new AsyncLock();

    private async Task NewCluster(Cluster cluster, string? clusterUpdateAckId)
    {
        var activeDeviceId = cluster.ActiveDeviceId;
        string? deviceName = null;
        if (cluster.Device.TryGetValue(activeDeviceId, out var activeDevice))
        {
            deviceName = activeDevice.Name;
        }

        SpotifyId trackId = default;
        if (!string.IsNullOrEmpty(cluster.PlayerState?.Track?.Uri))
        {
            trackId = SpotifyId.FromUri(cluster.PlayerState.Track.Uri);
        }

        bool isPaused = cluster.PlayerState?.IsPaused ?? true;

        var now = DateTimeOffset.UtcNow;
        var servertimestamp = DateTimeOffset.FromUnixTimeMilliseconds(cluster.Timestamp);
        var diff = now - servertimestamp;
        if (diff < TimeSpan.Zero)
        {
            diff = TimeSpan.Zero;
        }

        var positionSinceTimestamp = TimeSpan.FromMilliseconds(cluster.PlayerState?.PositionAsOfTimestamp ?? 0);
        var positionSinceSw = positionSinceTimestamp + diff;


        var stopwatch = isPaused ? new Stopwatch() : Stopwatch.StartNew();
        TimeSpan duration = TimeSpan.FromMilliseconds(cluster.PlayerState?.Duration ?? 0);
        if (positionSinceSw > duration)
        {
            positionSinceSw = duration;
            stopwatch.Stop();
        }

        RepeatMode repeatState = RepeatMode.Off;
        if (cluster.PlayerState?.Options?.RepeatingTrack is true)
        {
            repeatState = RepeatMode.Track;
        }
        else if (cluster.PlayerState?.Options?.RepeatingContext is true)
        {
            repeatState = RepeatMode.Context;
        }

        bool isShuffling = cluster.PlayerState?.Options?.ShufflingContext is true;
        string? contextUrl = cluster.PlayerState?.ContextUrl;
        string? contextUri = cluster.PlayerState?.ContextUri;
        if (string.IsNullOrEmpty(contextUrl) && !string.IsNullOrEmpty(contextUri))
        {
            contextUrl = $"context://{contextUri}";
        }

        SpotifyPlayableItem? currentTrack = null;
        if (!string.IsNullOrEmpty(cluster.PlayerState?.Track?.Uri))
        {
            using (await _tracksCacheLock.LockAsync())
            {
                if (!_trackCache.TryGetValue(SpotifyId.FromUri(cluster.PlayerState.Track.Uri), out currentTrack))
                {
                    var track = await _api.GetTrack(SpotifyId.FromUri(cluster.PlayerState.Track.Uri), true);
                    if (track is not null)
                    {
                        _trackCache[track.Id] = track;
                        currentTrack = track;
                    }
                }
            }
        }

        DateTimeOffset playingSinceTimestamp = DateTimeOffset.MinValue;
        playingSinceTimestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)cluster.TransferDataTimestamp);

        _playbackStateSubj.OnNext(new SpotifyRemotePlaybackState(
            deviceId: activeDeviceId,
            deviceName: deviceName,
            isPaused: isPaused,
            isBuffering: cluster.PlayerState?.IsBuffering ?? true,
            trackId: trackId,
            trackUid: cluster.PlayerState?.Track?.Uid,
            positionSinceSw: positionSinceSw,
            stopwatch: stopwatch,
            totalDuration: duration,
            repeatState: repeatState,
            isShuffling: isShuffling,
            contextUrl: contextUrl,
            contextUri: contextUri,
            currentTrack: currentTrack,
            clusterTimestamp: DateTimeOffset.FromUnixTimeMilliseconds(cluster.Timestamp),
            playingSinceTimestamp: playingSinceTimestamp,
            acknowledgmentId: clusterUpdateAckId
        ));
    }

    public Device NewState()
    {
        return new Device
        {
            PlayerState = _playerState,
            DeviceInfo = new DeviceInfo
            {
                CanPlay = true,
                Volume = (uint)((ushort.MaxValue) * 0.5),
                Name = _config.Playback.DeviceName,
                DeviceId = _config.Playback.DeviceId,
                DeviceType = _config.Playback.DeviceType,
                DeviceSoftwareVersion = "librespot-cs",
                ClientId = "65b708073fc0480ea92a077233ca87bd",
                SpircVersion = "3.2.6",
                Capabilities = new Capabilities
                {
                    CanBePlayer = true,
                    GaiaEqConnectId = true,
                    SupportsLogout = true,
                    IsObservable = true,
                    CommandAcks = true,
                    SupportsRename = true,
                    SupportsTransferCommand = true,
                    SupportsCommandRequest = true,
                    VolumeSteps = 1,
                    SupportsGzipPushes = true,
                    NeedsFullPlayerState = false,
                    SupportedTypes = { "audio/episode", "audio/track" }
                }
            }
        };
    }

    public void NewPutStateRequest(PutStateRequest putState)
    {
        _putStateRequest = putState;
    }

    public Task<Unit> RegisterAckId(string ackid)
    {
        TaskCompletionSource<Unit> tcs;
        using (_acknowledgementsLock.Lock())
        {
            if (_earlyAcknowledgements.Contains(ackid))
            {
                _earlyAcknowledgements.Remove(ackid);
                return Task.FromResult(Unit.Default);
            }

            tcs = new TaskCompletionSource<Unit>();
            _acknowledgements[ackid] = tcs;
            return tcs.Task;
        }
    }
}