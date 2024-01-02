using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify.connectstate;
using Wavee.Spotify.Core.Clients.Remote;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Utils;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.WebSocket;

namespace Wavee.Spotify.Infrastructure.WebSocket;

internal sealed partial class ActiveWebSocket : IDisposable
{
    private readonly ISpotifyWebSocketFactory _clientFactory;
    private readonly BlockingCollection<byte[]> _sendQueue;
    private readonly ISpotifyTokenService _tokenService;

    private Thread? _listenThread;
    private Thread? _sendThread;

    private ISpotifyWebSocket? _client;

    private readonly WaveeSpotifyConfig _config;

    private readonly SpotifyInternalHttpClient _apiClient;

    private readonly Func<SpotifyRemoteRequestRequest, Task<SpotifyRemoteRequestResult>> _onRequest;

    public ActiveWebSocket(ISpotifyWebSocketFactory clientFactory,
        ISpotifyTokenService tokenService,
        WaveeSpotifyConfig config,
        SpotifyInternalHttpClient apiClient,
        Func<SpotifyRemoteRequestRequest, Task<SpotifyRemoteRequestResult>> onRequest)
    {
        _clientFactory = clientFactory;
        _tokenService = tokenService;
        _config = config;
        _apiClient = apiClient;
        _onRequest = onRequest;

        _sendQueue = new();
    }

    public async Task<string> ConnectAsync(string host, ushort port, CancellationToken cancellationToken)
    {
        var client = _clientFactory.Create();
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        var url = $"wss://{host}:{port}/?access_token={token}";
        await client.ConnectAsync(url, cancellationToken);

        void onError(Exception x)
        {
            OnError?.Invoke(this, x);
        }

        var connectionId = await ReceiveConnectionId(client, cancellationToken);
        if (string.IsNullOrEmpty(connectionId))
        {
            throw new InvalidOperationException("ConnectionId was null or empty");
        }

        var playerState = new PlayerState();
        playerState.PlaybackSpeed = 1.0;
        playerState.SessionId = string.Empty;
        playerState.PlaybackId = string.Empty;
        playerState.Suppressions = new Suppressions();
        playerState.ContextRestrictions = new Restrictions();
        playerState.Options = new ContextPlayerOptions
        {
            RepeatingTrack = false,
            ShufflingContext = false,
            RepeatingContext = false
        };
        playerState.Position = 0;
        playerState.PositionAsOfTimestamp = 0;
        playerState.IsPlaying = false;
        playerState.IsSystemInitiated = true;

        var putState = playerState.ToPutState(PutStateReason.NewDevice,
            volume: _config.Playback.InitialVolume,
            playerPosition: null,
            hasBeenPlayingSince: null,
            now: DateTimeOffset.UtcNow,
            lastCommandSentBy: null,
            lastCommandId: null,
            _config.Remote);

        var initialCluster = await _apiClient.PutStateAsync(
            connectionId: connectionId,
            putState,
            cancellationToken);

        Cluster = initialCluster;
        OnClusterUpdate?.Invoke(this, new ClusterUpdate
        {
            Cluster = initialCluster,
            UpdateReason = ClusterUpdateReason.DeviceNewConnection
        });
        _listenThread = new Thread(() => ListenLoop(client, onError, _onRequest, cluster =>
        {
            Cluster = cluster.Cluster;
            OnClusterUpdate?.Invoke(this, cluster);
        }));
        _listenThread.Start();

        _sendThread = new Thread(() => SendLoop(client, _sendQueue));
        _sendThread.Start();

        _client = client;

        return connectionId;
    }


    public Cluster? Cluster { get; private set; }

    public event EventHandler<ClusterUpdate> OnClusterUpdate;


    private static void ListenLoop(
        ISpotifyWebSocket stream,
        Action<Exception> onError,
        Func<SpotifyRemoteRequestRequest, Task<SpotifyRemoteRequestResult>> onRequest,
        Action<ClusterUpdate> onCluster)
    {
        try
        {
            while (stream.Connected)
            {
                using var message = ReadNextMessage(stream, CancellationToken.None).Result;

                // Process received message

                var headers = new Dictionary<string, string>();
                if (message.RootElement.TryGetProperty("headers", out var headersElement))
                {
                    using var enumerator = headersElement.EnumerateObject();
                    // headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
                    foreach (var curr in enumerator)
                    {
                        headers.Add(curr.Name, curr.Value.GetString()!);
                    }
                }


                var type = message.RootElement.GetProperty("type").GetString();
                var payload = ReadPayload(message.RootElement, headers);


                switch (type)
                {
                    case "pong":
                        break;
                    case "ping":
                        break;
                    case "request":
                    {
                        var req = new SpotifyRemoteRequestRequest
                        {
                            Key = message.RootElement.GetProperty("key").GetString()!,
                            Data = payload
                        };
                        var result = onRequest(req).Result;
                        var reply = string.Format(
                            "{{\"type\":\"reply\", \"key\": \"{0}\", \"payload\": {{\"success\": {1}}}}}",
                            req.Key, result.Success switch
                            {
                                true => "true",
                                false => "false"
                            });
                        stream.SendAsync(reply, CancellationToken.None).Wait();
                        break;
                    }
                    default:
                    {
                        //message
                        var uri = message.RootElement.GetProperty("uri").GetString();
                        if (string.Equals(uri, "hm://connect-state/v1/cluster"))
                        {
                            var update = ClusterUpdate.Parser.ParseFrom(payload.Span);
                            onCluster(update);
                        }

                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions and possibly trigger reconnection
            onError(ex);
        }
    }

    private static ReadOnlyMemory<byte> ReadPayload(JsonElement messageRootElement, Dictionary<string, string> headers)
    {
        Memory<byte> payload = Memory<byte>.Empty;
        var gzip = false;
        if (headers.TryGetValue("Transfer-Encoding", out var trnsfEncoding))
        {
            if (trnsfEncoding is "gzip")
            {
                gzip = true;
            }
        }

        if (messageRootElement.TryGetProperty("payloads", out var payloadsArr))
        {
            var payloads = new ReadOnlyMemory<byte>[payloadsArr.GetArrayLength()];
            for (var i = 0; i < payloads.Length; i++)
            {
                payloads[i] = payloadsArr[i].GetBytesFromBase64();
            }

            var totalLength = payloads.Sum(p => p.Length);
            payload = new byte[totalLength];
            var offset = 0;
            foreach (var payloadPart in payloads)
            {
                payloadPart.CopyTo(payload.Slice(offset));
                offset += payloadPart.Length;
            }
        }
        else if (messageRootElement.TryGetProperty("payload", out var payloadStr))
        {
            if (gzip is true)
            {
                payload = payloadStr.GetProperty("compressed").GetBytesFromBase64();
            }
            else
            {
                payload = payloadStr.GetBytesFromBase64();
            }
        }
        else
        {
            payload = Memory<byte>.Empty;
        }

        switch (gzip)
        {
            case false:
                //do nothing
                break;
            case true:
            {
                payload = Gzip.UnsafeDecompressAltAsMemory(payload.Span);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }


        return payload;
    }

    private static void SendLoop(
        ISpotifyWebSocket client,
        BlockingCollection<byte[]> send)
    {
    }

    private async Task<string?> ReceiveConnectionId(ISpotifyWebSocket client, CancellationToken cancellationToken)
    {
        using var message = await ReadNextMessage(client, cancellationToken);
        if (message is null)
        {
            return null;
        }

        var connectionIdRegex = ConnIdRegex();

        var root = message.RootElement;
        var uri = root.GetProperty("uri").GetString();
        var match = connectionIdRegex.Match(uri);
        if (match.Success)
        {
            var headers = root.GetProperty("headers");
            var connectionId = headers.GetProperty("Spotify-Connection-Id").GetString();
            return connectionId;
        }

        return null;
    }

    private static async Task<JsonDocument?> ReadNextMessage(ISpotifyWebSocket client,
        CancellationToken cancellationToken)
    {
        using var message = new MemoryStream();
        bool endOfMessage = false;
        while (!endOfMessage)
        {
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);
            var result = await client.ReceiveAsync(segment, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
            endOfMessage = result.EndOfMessage;
        }

        message.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);
    }

    public event EventHandler<Exception>? OnError;
    public bool Connected => _client?.State is WebSocketState.Open;

    public void Dispose()
    {
        _sendQueue.Dispose();
        _client?.Dispose();
    }

    [GeneratedRegex(@"hm://pusher/v1/connections/([^/]+)")]
    private static partial Regex ConnIdRegex();
}