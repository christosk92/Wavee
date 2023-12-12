using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Eum.Spotify.playback;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Remote.Commands;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Utils;
using ContextPlayerOptions = Eum.Spotify.connectstate.ContextPlayerOptions;
using PlayOrigin = Eum.Spotify.context.PlayOrigin;
using Restrictions = Eum.Spotify.connectstate.Restrictions;

namespace Wavee.Spotify.Application.Remote;

internal sealed partial class SpotifyRemoteHolder : ISpotifyRemoteClient
{
    private (Cluster Cluster, string ConnectionId)? _remoteState;
    private readonly SpotifyLocalState _localState;

    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    private readonly SpotifyClientConfig _config;
    private SpotifyWebsocketHolder? _websocket;

    public SpotifyRemoteHolder(SpotifyClientConfig config, IMediator mediator, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _mediator = mediator;

        _localState = SpotifyLocalState.Empty(config);
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }
    public event EventHandler<Cluster>? RemoteStateChanged;
    public event EventHandler<IReadOnlyCollection<SpotifyLibraryItem<SpotifyId>>>? ItemAdded;
    public event EventHandler<IReadOnlyCollection<SpotifyId>>? ItemRemoved;

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        var token = await _mediator.Send(new GetSpotifyTokenQuery(), cancellationToken);
        var url = $"wss://gae2-dealer.spotify.com:443/?access_token={token}";
        _websocket = await SpotifyWebsocketHolder.Connect(url, cancellationToken);
        _websocket.Disconnected += OnDisconnected;
        _websocket.ConnectionId += OnConnectionId;
        _websocket.RemoteClusterUpdate += OnRemoteClusterUpdate;
        _websocket.LibraryUpdate += OnLibraryUpdate;

        await Task.Factory.StartNew(async () =>
        {
            await Task.Delay(10, cancellationToken);
            await _websocket.Listen();
        }, cancellationToken);
    }

    private void OnLibraryUpdate(object? sender, (string, JsonElement) e)
    {
        try
        {
            if (e.Item1.StartsWith("hm://collection/artist/"))
            {
                //Artist update
                using var payloads = e.Item2.GetProperty("payloads").EnumerateArray();
                payloads.MoveNext();
                var payloadStr = payloads.Current.GetString();
                using var json = JsonDocument.Parse(payloadStr);
                var payload = json.RootElement;
                var items = payload.GetProperty("items");

                var removedItems = new SpotifyId[items.GetArrayLength()];
                var added = new SpotifyLibraryItem<SpotifyId>[items.GetArrayLength()];
                var i = 0;
                foreach (var item in items.EnumerateArray())
                {
                    ReadOnlySpan<char> id = item.GetProperty("identifier").GetString();
                    var spotifyId = SpotifyId.FromBase62(id, SpotifyItemType.Artist);
                    var removed = item.GetProperty("removed").GetBoolean();
                    if (removed)
                    {
                        removedItems[i++] = spotifyId;
                    }
                    else
                    {
                        var addedAt = item.GetProperty("addedAt").GetInt64();
                        added[i++] = new SpotifyLibraryItem<SpotifyId>
                        {
                            Item = spotifyId,
                            AddedAt = DateTimeOffset.FromUnixTimeSeconds(addedAt),
                            LastPlayedAt = null
                        };
                    }
                }

                added = added.Where(x => x is not null).ToArray();
                if (added.Length > 0)
                {
                    ItemAdded?.Invoke(this, added);
                }

                added = added.Where(x => x is not null).ToArray();
                if (removedItems.Length > 0)
                {
                    ItemRemoved?.Invoke(this, removedItems);
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private async void OnRemoteClusterUpdate(object? sender, ClusterUpdate e)
    {
        _remoteState = (e.Cluster, _remoteState?.ConnectionId);
        RemoteStateChanged?.Invoke(this, e.Cluster);
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
        RemoteStateChanged?.Invoke(this, result);
    }

    private async void OnDisconnected(object? sender, Exception e)
    {
        if (sender is SpotifyWebsocketHolder spotifyWebsocketHolder)
        {
            spotifyWebsocketHolder.Disconnected -= OnDisconnected;
            spotifyWebsocketHolder.ConnectionId -= OnConnectionId;
            spotifyWebsocketHolder.RemoteClusterUpdate -= OnRemoteClusterUpdate;
            spotifyWebsocketHolder.LibraryUpdate -= OnLibraryUpdate;

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

            return client;
        }

        internal async Task Listen()
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
                            var headers = root.GetProperty("headers");
                            var payload = root.GetProperty("payloads");
                            var sw = Stopwatch.StartNew();
                            var clusterUpdate = ParseClusterUpdate(payload, headers);
                            RemoteClusterUpdate?.Invoke(this, clusterUpdate);
                            sw.Stop();
                        }
                        else
                        {
                            //something else
                            if (uri.StartsWith("hm://collection") && uri.EndsWith("/json"))
                            {
                                LibraryUpdate?.Invoke(this, (uri, root.Clone()));
                            }
                            else
                            {

                            }
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

        private static ClusterUpdate ParseClusterUpdate(JsonElement payload, JsonElement headers)
        {
            bool isGzip = headers.GetProperty("Transfer-Encoding").GetString() is "gzip";

            ReadOnlySpan<byte> output = stackalloc byte[0];
            using var enu = payload.EnumerateArray();
            while (enu.MoveNext())
            {
                var item = enu.Current;
                Span<byte> buffer = item.GetBytesFromBase64();
                //Add to output
                Span<byte> newOutput = stackalloc byte[output.Length + buffer.Length];
                output.CopyTo(newOutput);
                buffer.CopyTo(newOutput.Slice(output.Length));
                output = newOutput;
            }

            if (isGzip)
            {
                output = Gzip.UnsafeDecompressAlt(output);
            }

            var cluster = ClusterUpdate.Parser.ParseFrom(output);
            return cluster;
        }


        public void Dispose()
        {
            _webSocket.Dispose();
            _cts.Cancel();
        }

        public event EventHandler<string> ConnectionId;
        public event EventHandler<Exception> Disconnected;
        public event EventHandler<ClusterUpdate> RemoteClusterUpdate;
        public event EventHandler<(string, JsonElement)>? LibraryUpdate;


        [GeneratedRegex(@"hm://pusher/v1/connections/([^/]+)")]
        private static partial Regex ConnIdRegex();
    }

    public Task Play(Context context, PlayOrigin playOrigin, PreparePlayOptions preparePlayOptions)
    {
        JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation().WithPreserveProtoFieldNames(true
            ));


        var ctxJson = JsonSerializer.Deserialize<object>(formatter.Format(context));
        var playOriginJson = JsonSerializer.Deserialize<object>(formatter.Format(playOrigin));
        var preparePlayOptionsJson = JsonSerializer.Deserialize<object>(formatter.Format(preparePlayOptions));
        var body = new
        {
            command = new
            {
                endpoint = "play",
                context = ctxJson,
                play_origin = playOriginJson,
                options = preparePlayOptionsJson
            }
        };

        ReadOnlyMemory<byte> json = JsonSerializer.SerializeToUtf8Bytes(body);
        return Command(json);
    }

    private async Task Command(ReadOnlyMemory<byte> json)
    {
        const string url =
            "https://spclient.com/connect-state/v1/player/command/from/{0}/to/{1}";
        var ownDeviceId = _config.Remote.DeviceId;
        var toDevice = _remoteState?.Cluster.ActiveDeviceId;
        if (toDevice is null)
        {
            throw new InvalidOperationException("No active device");
        }

        var formattedUrl = string.Format(url, ownDeviceId, toDevice);
        using var readOnlyMemoryContent = new ReadOnlyMemoryContent(json);
        readOnlyMemoryContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, formattedUrl)
        {
            Content = readOnlyMemoryContent
        };
        using var r = await _httpClient.SendAsync(req, CancellationToken.None);
        r.EnsureSuccessStatusCode();
    }
}

internal readonly record struct SpotifyLocalState
{
    public DateTimeOffset StartedPlayingAt { get; init; }
    public uint? LastCommandId { get; init; }
    public string? LastCommandSentByDeviceId { get; init; }

    private const uint VOLUME_STEPS = 12;
    public const uint MAX_VOLUME = 65535;

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
            PositionAsOfTimestamp = 0,
            Position = 0,
            PlaybackSpeed = 1.0,
            IsPlaying = false
        };
    }

    public static SpotifyLocalState Empty(SpotifyClientConfig config)
    {
        return new SpotifyLocalState();
    }
}