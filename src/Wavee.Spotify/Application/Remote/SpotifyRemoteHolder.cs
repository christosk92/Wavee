using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify.connectstate;
using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Remote.Commands;
using Websocket.Client;

namespace Wavee.Spotify.Application.Remote;

internal sealed partial class SpotifyRemoteHolder : ISpotifyRemoteClient
{
    private (Cluster Cluster, string ConnectionId)? _remoteState;
    private readonly SpotifyLocalState _localState;

    private readonly IMediator _mediator;
    private readonly SpotifyClientConfig _config;
    private SpotifyWebsocketHolder? _websocket;

    public SpotifyRemoteHolder(SpotifyClientConfig config, IMediator mediator)
    {
        _config = config;
        _mediator = mediator;

        _localState = SpotifyLocalState.Empty(config);
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        var token = await _mediator.Send(new GetSpotifyTokenQuery(), cancellationToken);
        var url = $"wss://gae2-dealer.spotify.com:443/?access_token={token}";
        _websocket = await SpotifyWebsocketHolder.Connect(url, cancellationToken);
        _websocket.Disconnected += OnDisconnected;
        _websocket.ConnectionId += OnConnectionId;
    }

    private async void OnConnectionId(object? sender, string e)
    {
        var localState = _localState.BuildPutState(_config,
            PutStateReason.NewDevice,
            null,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); //TODO: Get timestamp from somewhere

        var result = await _mediator.Send(new SpotifyRemotePutDeviceStateCommand
        {
            State = localState,
            ConnectionId = e
        }, CancellationToken.None);

        _remoteState = (result, e);
    }

    private async void OnDisconnected(object? sender, Exception e)
    {
        if (sender is SpotifyWebsocketHolder spotifyWebsocketHolder)
        {
            spotifyWebsocketHolder.Disconnected -= OnDisconnected;
            spotifyWebsocketHolder.ConnectionId -= OnConnectionId;
            spotifyWebsocketHolder.Dispose();
        }
        await Task.Delay(TimeSpan.FromSeconds(5));
        await Initialize();
    }

    private sealed partial class SpotifyWebsocketHolder : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;

        private SpotifyWebsocketHolder(ClientWebSocket clientWebSocket, CancellationTokenSource cts)
        {
            _webSocket = clientWebSocket;
            _cts = cts;
        }

        public static async Task<SpotifyWebsocketHolder> Connect(string url, CancellationToken ct)
        {
            CancellationTokenSource cts = new();
            ClientWebSocket clientWebSocket = new();
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
            clientWebSocket.Options.SetRequestHeader("Origin",
                "https://open.spotify.com");
            await clientWebSocket.ConnectAsync(new Uri(url), ct);
            var client = new SpotifyWebsocketHolder(clientWebSocket, cts);

            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10, cts.Token);
                await client.Listen();
            }, cts.Token);
            return client;
        }

        private async Task Listen()
        {
            try
            {
                //hm://pusher/v1/connections/NjdjMTZjYjYtYmNkNS00MDRhLTk0YjgtZDFlNDE1MWNmOWEwK2RlYWxlcit0Y3A6Ly9nYWUyLWRlYWxlci1hLXg1OGIuZ2FlMi5zcG90aWZ5Lm5ldDo1NzAwKzg3M0NBMjdCRUMyRTVBRTFCRkZDOEI4RTA5RTI4MjVCODY5NkIxMjVBMDA0QkZGQkEzNjZDMUI2QUFBMjA0MjA%3D
                var connectionIdRegex = ConnIdRegex();

                while (!_cts.IsCancellationRequested)
                {
                    using var message = new MemoryStream();
                    bool endOfMessage = false;
                    while (!endOfMessage)
                    {
                        var buffer = new byte[1024 * 4];
                        var segment = new ArraySegment<byte>(buffer);
                        var result = await _webSocket.ReceiveAsync(segment, _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _cts.CancelAsync();
                        }

                        message.Write(buffer, 0, result.Count);
                        endOfMessage = result.EndOfMessage;
                    }

                    message.Seek(0, SeekOrigin.Begin);
                    using var jsondoc = await JsonDocument.ParseAsync(message, cancellationToken: _cts.Token);
                    var root = jsondoc.RootElement;
                    var uri = root.GetProperty("uri").GetString();
                    //    "uri": "hm://pusher/v1/connections/NjdjMTZjYjYtYmNkNS00MDRhLTk0YjgtZDFlNDE1MWNmOWEwK2RlYWxlcit0Y3A6Ly9nYWUyLWRlYWxlci1hLXg1OGIuZ2FlMi5zcG90aWZ5Lm5ldDo1NzAwKzg3M0NBMjdCRUMyRTVBRTFCRkZDOEI4RTA5RTI4MjVCODY5NkIxMjVBMDA0QkZGQkEzNjZDMUI2QUFBMjA0MjA%3D"
                    var match = connectionIdRegex.Match(uri);
                    if (match.Success)
                    {
                        var headers = root.GetProperty("headers");
                        var connectionId = headers.GetProperty("Spotify-Connection-Id").GetString();
                        ConnectionId?.Invoke(this, connectionId);
                    }
                    else
                    {
                        //something else
                        if (uri.Equals("hm://connect-state/v1/cluster"))
                        {
                            //Cluster update
                            
                        }
                        else
                        {
                            //something else
                        }
                    }
                }

                Disconnected?.Invoke(this, new Exception("Websocket disconnected"));
            }
            catch (Exception e)
            {
                if (e is not ObjectDisposedException)
                {
                    Disconnected?.Invoke(this, e);
                }
            }
        }

        public void Dispose()
        {
            _webSocket.Dispose();
            _cts.Cancel();
        }

        public event EventHandler<string> ConnectionId;
        public event EventHandler<Exception> Disconnected;

        [GeneratedRegex(@"hm://pusher/v1/connections/([^/]+)")]
        private static partial Regex ConnIdRegex();
    }
}

internal readonly record struct SpotifyLocalState
{
    public DateTimeOffset StartedPlayingAt { get; init; }
    public uint? LastCommandId { get; init; }
    public string? LastCommandSentByDeviceId { get; init; }

    private const uint VOLUME_STEPS = 12;
    private const uint MAX_VOLUME = 65535;

    public PutStateRequest BuildPutState(
        SpotifyClientConfig config,
        PutStateReason reason,
        long? playerTime,
        long timestamp)
    {
        var putState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                PlayerState = BuildPlayerState(),
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = (uint)(config.Playback.InitialVolume * MAX_VOLUME),
                    Name = config.Remote.DeviceName,
                    DeviceId = config.Remote.DeviceId,
                    DeviceType = config.Remote.DeviceType,
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
                        NeedsFullPlayerState = false,
                        SupportedTypes = { "audio/episode", "audio/track" }
                    }
                }
            },
            HasBeenPlayingForMs =
                playerTime switch
                {
                    { } t => (ulong)Math.Min(t, timestamp - StartedPlayingAt.ToUnixTimeMilliseconds()),
                    null => (ulong)0
                },
            PutStateReason = reason,
            ClientSideTimestamp = (ulong)timestamp,
            LastCommandMessageId = LastCommandId ?? 0,
            LastCommandSentByDeviceId = LastCommandSentByDeviceId ?? string.Empty
        };

        return putState;
    }

    private PlayerState BuildPlayerState()
    {
        return new PlayerState
        {
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingContext = false,
                RepeatingTrack = false,
                ShufflingContext = false
            },
            PositionAsOfTimestamp = 0, Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }

    public static SpotifyLocalState Empty(SpotifyClientConfig config)
    {
        return new SpotifyLocalState();
    }
}

public interface ISpotifyRemoteClient
{
}