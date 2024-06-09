using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Eum.Spotify.spircs;
using Google.Protobuf;
using OneOf;
using Wavee.Contracts.Common;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Clients;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Interfaces;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Remote;

namespace Wavee.UI.Spotify.Playback;

internal class SpotifyPlaybackDevice : IPlaybackDevice
{
    private readonly string _deviceId;
    private string _name;
    private DeviceType _type;
    private readonly ISpClient _spClient;
    private readonly SpotifyTokenClient _tokensClient;
    private readonly ISpotifyWebsocketConnectionFactory _connectionFactory;

    private ISpotifyWebsocketConnection? _connection;
    private string? _connectionId;

    private readonly SpotifyClient _parentClient;

    private readonly BehaviorSubject<PlaybackConnectionStatusType> _connectionStatusSubj =
        new(PlaybackConnectionStatusType.Disconnected);

    private readonly SpotifyMessageHandler _messageHandler = new();
    private readonly SpotifyRequestHandler _requestHandler = new();

    public SpotifyPlaybackDevice(string deviceId,
        string name,
        DeviceType type,
        ISpClient spClient,
        SpotifyTokenClient tokensClient,
        SpotifyClient parentClient,
        ISpotifyWebsocketConnectionFactory connectionFactory)
    {
        _deviceId = deviceId;
        _name = name;
        _type = type;
        _spClient = spClient;
        _tokensClient = tokensClient;
        _parentClient = parentClient;
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> Connect(CancellationToken cancellationToken)
    {
        if (_connection is { Connected: true })
            return false;

        await ConnectInternal(cancellationToken);
        return true;
    }

    private async Task ConnectInternal(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            _connection.Disconnected -= ConnectionOnDisconnected;
            _connection.Dispose();
            _connection = null;
        }

        var token = await _tokensClient.GetToken(cancellationToken);
        var url = $"wss://{Dealer}?access_token={token}";
        _connection = _connectionFactory.Create(url, _messageHandler, _requestHandler);
        _connection.Disconnected += ConnectionOnDisconnected;
        _connectionStatusSubj.OnNext(PlaybackConnectionStatusType.Connecting);
        _connectionId = await _connection.Connect(cancellationToken);
        var state = BuildState();
        var latestCluster = await UpdateState(
            deviceId: _deviceId,
            deviceName: _name,
            deviceType: _type,
            reason: PutStateReason.NewDevice,
            state: state,
            startedPlayingAt: null,
            cancellationToken
        );
        var internalJson = new
        {
            data = latestCluster.ToByteString().ToBase64()
        };
        using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(internalJson));
        _messageHandler.HandleUri(SpotifyMessageHandler.InternalClusterUpdate, jsonDocument.RootElement, []);
        _connectionStatusSubj.OnNext(PlaybackConnectionStatusType.Connected);
    }

    private PlayerState BuildState()
    {
        return new PlayerState
        {
            PlaybackSpeed = 1.0,
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingTrack = false,
                ShufflingContext = false,
                RepeatingContext = false
            },
            Position = 0,
            PositionAsOfTimestamp = 0,
            IsPlaying = false,
            IsSystemInitiated = true
        };
    }

    private async Task<OneOf<StateError, IPlaybackState>> Mutate(Cluster cluster)
    {
        // _cluster.OnNext(e);
        try
        {
            var timestamp = cluster.PlayerState.Timestamp;
            var posSinceTs = cluster.PlayerState.PositionAsOfTimestamp;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var diff = now - timestamp;
            var pos = TimeSpan.FromMilliseconds(diff + posSinceTs);
            var stopwatch = cluster.PlayerState.IsPlaying && !cluster.PlayerState.IsPaused
                ? Stopwatch.StartNew()
                : new Stopwatch();

            IPlayableItem item = null;
            if (RegularSpotifyId.TryParse(cluster.PlayerState?.Track?.Uri, out var spotifyId))
            {
                item = spotifyId.Type switch
                {
                    SpotifyIdItemType.Track => await _parentClient.Tracks.GetTrack(spotifyId, CancellationToken.None),
                    SpotifyIdItemType.Episode => await _parentClient.Episodes.GetEpisode(spotifyId,
                        CancellationToken.None),
                    _ => null
                };

                // var vorbisFile360 = item?.AudioFiles.FirstOrDefault(y => y.Quality is AudioFileQuality.High);
                // var key = await _tokensClient.GetAudioKey(spotifyId, vorbisFile360.Id, CancellationToken.None);
            }

            SpotifyContextInfo? context = null;
            if (!string.IsNullOrEmpty(cluster.PlayerState?.ContextUri))
            {
            }

            RemotePlaybackStateType stateType = RemotePlaybackStateType.Stopped;
            if (cluster.PlayerState is null || !cluster.PlayerState.IsPlaying)
            {
                stateType = RemotePlaybackStateType.Stopped;
            }
            else
                stateType = cluster.PlayerState.IsPlaying switch
                {
                    true when cluster.PlayerState.IsPaused => RemotePlaybackStateType.Paused,
                    true when !cluster.PlayerState.IsPaused => RemotePlaybackStateType.Playing,
                    _ => stateType
                };

            var realTimePosition = RealTimePosition.Create(pos, stopwatch);
            var state = new SpotifyPlaybackState(item, realTimePosition, stateType);
            return state;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new StateError(e);
        }
    }

    private async void ConnectionOnDisconnected(object sender, (Exception, WebSocketCloseStatus?) e)
    {
        if (e.Item1 is not null)
        {
            Console.WriteLine(e.Item1.Message);
            _connectionStatusSubj.OnNext(PlaybackConnectionStatusType.Error);

            while (true)
            {
                try
                {
                    await ConnectInternal(CancellationToken.None);
                    break;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
                    continue;
                }
            }
        }
        else
        {
            _connectionStatusSubj.OnNext(PlaybackConnectionStatusType.Disconnected);
        }
    }

    public async Task<Cluster> UpdateState(string deviceId,
        string deviceName,
        DeviceType deviceType,
        PutStateReason reason,
        PlayerState? state,
        DateTimeOffset? startedPlayingAt,
        CancellationToken cancel)
    {
        const uint VOLUME_STEPS = 12;
        const uint MAX_VOLUME = 65535;
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(0.5 * MAX_VOLUME),
                    Name = deviceName,
                    DeviceId = deviceId,
                    DeviceType = deviceType,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,

                        VolumeSteps = (int)VOLUME_STEPS,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsCommandRequest = true,
                        SupportsTransferCommand = true,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = true,
                        SupportsHifi = new CapabilitySupportDetails
                        {
                            DeviceSupported = false,
                            FullySupported = false,
                            UserEligible = false
                        }, // TODO: Hifi
                        SupportedTypes =
                        {
                            "audio/episode",
                            "audio/track",
                            //"audio/local"
                        }
                    }
                }
            },
            PutStateReason = reason,
            ClientSideTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastCommandMessageId = 0,
            LastCommandSentByDeviceId = string.Empty
        };
        if (startedPlayingAt != null)
        {
            putState.StartedPlayingAt = (ulong)startedPlayingAt.Value.ToUnixTimeMilliseconds();
            putState.IsActive = true;
        }

        if (state != null)
        {
            putState.Device.PlayerState = state;
        }

        var connectionId = _connectionId;
        return await _spClient.PutState(putState, deviceId, connectionId, cancellationToken: cancel);
    }

    private const string Dealer = "gae2-dealer.spotify.com";
    private const int Port = 443;

    public IObservable<OneOf<StateError, IPlaybackState>> State => _messageHandler
        .Cluster
        .Throttle(TimeSpan.FromMilliseconds(100))
        .SelectMany(async c => await Mutate(c));

    public IObservable<PlaybackConnectionStatusType> ConnectionStatus =>
        _connectionStatusSubj.DistinctUntilChanged();
}