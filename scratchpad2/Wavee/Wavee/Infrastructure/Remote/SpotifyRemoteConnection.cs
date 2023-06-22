using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Serilog;
using Wavee.Infrastructure.Connection;
using Wavee.Playback;
using Wavee.Remote;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Infrastructure.Remote;

internal static class SpotifyRemoteConnection
{
    private record SpotifyRemoteConnectionInfo(ClientWebSocket Stream,
        string RemoteConnectionId,
        Cluster? LatestCluster,
        ChannelWriter<ReadOnlyMemory<byte>> Sender,
        Func<RemoteMessageReceiveCondition, SpotifyRemoteMessageCallback> Receiver,
        List<SpotifyRemoteMessageCallbackFull> Callbacks);

    private readonly record struct SpotifyRemoteMessageCallbackFull(Channel<SpotifyRemoteMessage> Reader,
        RemoteMessageReceiveCondition PackageReceiveCondition, Action<SpotifyRemoteMessage> Incoming);

    private static readonly Dictionary<Guid, SpotifyRemoteConnectionInfo> Connections = new();

    public static SpotifyRemoteMessageCallback CreateListener(this Guid connectionId,
        RemoteMessageReceiveCondition condition)
    {
        var connection = Connections[connectionId];
        return connection.Receiver(condition);
    }

    public static async Task PutState(
        Guid connectionId,
        SpotifyLocalPlaybackState localState,
        CancellationToken ct = default)
    {
        var connection = Connections[connectionId];
        var putState = localState.BuildPutStateRequest(
            reason: PutStateReason.PlayerStateChanged,
            playerTime: Option<TimeSpan>.None
        );
        var remoteConnId = connection.RemoteConnectionId;
        var jwt = await SpotifyClient.Clients[connectionId].Token.GetToken(ct);
        var deviceId = localState.DeviceId;
        await Put(
            putState,
            remoteConnId,
            jwt,
            deviceId, ct);
    }

    private static void SetupConnectionListener(
        ClientWebSocket webSocket,
        Cluster firstCluster,
        string remoteConnectionId,
        Guid connectionId,
        Action<Exception> onConnectionLost)
    {
        var mainChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();

        object _lock = new();
        var packages = new List<SpotifyRemoteMessage>();

        var connectionInfo = new SpotifyRemoteConnectionInfo(
            webSocket,
            remoteConnectionId,
            firstCluster,
            mainChannel,
            (condition) =>
            {
                lock (_lock)
                {
                    //setup new callback
                    var newChannel = Channel.CreateUnbounded<SpotifyRemoteMessage>();
                    var callbackFull =
                        new SpotifyRemoteMessageCallbackFull(newChannel, condition,
                            (pkg) => { newChannel.Writer.TryWrite(pkg); });
                    var callback = new SpotifyRemoteMessageCallback(newChannel.Reader, () =>
                    {
                        Connections[connectionId].Callbacks.Remove(callbackFull);
                        newChannel.Writer.TryComplete();
                    });
                    Connections[connectionId].Callbacks.Add(callbackFull);

                    //check if we have any packages that match the condition already
                    foreach (var package in packages)
                    {
                        if (condition(package))
                        {
                            callbackFull.Incoming(package);
                        }
                    }

                    return callback;
                }
            },
            new List<SpotifyRemoteMessageCallbackFull>());

        Connections[connectionId] = connectionInfo;

        object lockObj = new();
        bool alreadyReconnected = false;
        IDisposable? pingPong = null;

        //Setup sender
        Task.Factory.StartNew(async () =>
        {
            try
            {
                await foreach (var pkg in mainChannel.Reader.ReadAllAsync())
                {
                    //write 
                    var buffer = new ArraySegment<byte>(pkg.ToArray());
                    await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                lock (lockObj)
                {
                    if (!alreadyReconnected)
                    {
                        alreadyReconnected = true;
                        CloseConnection(connectionId);
                        onConnectionLost(e);
                        pingPong?.Dispose();
                    }
                }
            }
        });

        //write first cluster
        var firstClusterUpdate = new ClusterUpdate
        {
            Cluster = firstCluster
        };

        foreach (var callback in connectionInfo.Callbacks)
        {
            if (callback.PackageReceiveCondition(new SpotifyRemoteMessage(
                    Type: SpotifyRemoteMessageType.Message,
                    Uri: "hm://connect-state/v1/cluster",
                    Payload: ReadOnlyMemory<byte>.Empty
                )))
            {
                callback.Incoming(new SpotifyRemoteMessage(
                    Type: SpotifyRemoteMessageType.Message,
                    Uri: "hm://connect-state/v1/cluster",
                    Payload: firstClusterUpdate.ToByteArray()
                ));
            }
        }

        //setup timer (every 30 seconds)
        pingPong = Observable
            .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .SelectMany(async _ =>
            {
                var ping = new
                {
                    type = "ping"
                };
                ReadOnlyMemory<byte> json = JsonSerializer.SerializeToUtf8Bytes(ping);
                await mainChannel.Writer.WriteAsync(json);
                return Unit.Default;
            })
            .Subscribe();

        //Setup reader
        Task.Factory.StartNew(async () =>
        {
            try
            {
                while (true)
                {
                    var nextMessage = await WebsocketIO.Receive(webSocket, CancellationToken.None);
                    using var json = JsonDocument.Parse(nextMessage);
                    var type = json.RootElement.GetProperty("type").GetString();

                    switch (type)
                    {
                        case "pong":
                            Log.Logger.Debug("Pong received");
                            break;
                        case "message":
                        {
                            lock (_lock)
                            {
                                static ReadOnlyMemory<byte> Parse(JsonDocument json)
                                {
                                    var payloads = json.RootElement.GetProperty("payloads");
                                    using var headersJson = json.RootElement.GetProperty("headers").EnumerateObject();
                                    var headers = new Dictionary<string, string>();
                                    foreach (var header in headersJson)
                                    {
                                        headers.Add(header.Name, header.Value.GetString()!);
                                    }

                                    if (headers.ContainsKey("Transfer-Encoding") ||
                                        headers.ContainsKey("Content-Type") &&
                                        headers["Content-Type"] is "application/octet-stream")
                                    {
                                        using var enumerator = payloads.EnumerateArray();
                                        using var buffer = new MemoryStream();
                                        foreach (var element in enumerator)
                                        {
                                            ReadOnlySpan<byte> bytes = element.GetBytesFromBase64();
                                            buffer.Write(bytes);
                                        }

                                        buffer.Flush();
                                        buffer.Seek(0, SeekOrigin.Begin);
                                        if (headers.ContainsKey("Transfer-Encoding") &&
                                            headers["Transfer-Encoding"] is "gzip")
                                        {
                                            using var gzipDecoded = GzipHelpers.GzipDecompress(buffer);
                                            gzipDecoded.Seek(0, SeekOrigin.Begin);
                                            return gzipDecoded.ToArray();
                                        }

                                        return buffer.ToArray();
                                    }
                                    else if (headers.ContainsKey("Content-Type") &&
                                             headers["Content-Type"] is "application/json")
                                    {
                                        return Encoding.UTF8.GetBytes(payloads.GetRawText());
                                    }
                                    else if (headers.ContainsKey("Content-Type") &&
                                             headers["Content-Type"] is "text/plain")
                                    {
                                        return Encoding.UTF8.GetBytes(payloads.GetRawText());
                                    }
                                    else
                                    {
                                        return Encoding.UTF8.GetBytes(payloads.GetRawText());
                                    }
                                }

                                var parsed = Parse(json);
                                var pkg = new SpotifyRemoteMessage(
                                    Type: SpotifyRemoteMessageType.Message,
                                    Uri: json.RootElement.GetProperty("uri").GetString()!,
                                    Payload: parsed
                                );
                                if (pkg.Uri.StartsWith("hm://connect-state/v1/cluster"))
                                {
                                    //update cluster
                                    var clusterUpdate = ClusterUpdate.Parser.ParseFrom(pkg.Payload.Span);
                                    lock (_lock)
                                    {
                                        Connections[connectionId] = Connections[connectionId] with
                                        {
                                            LatestCluster = clusterUpdate.Cluster
                                        };
                                    }
                                }

                                var wasInteresting = false;
                                foreach (var callback in connectionInfo.Callbacks)
                                {
                                    if (callback.PackageReceiveCondition(pkg))
                                    {
                                        callback.Incoming(pkg);
                                        wasInteresting = true;
                                    }
                                }

                                if (!wasInteresting)
                                {
                                    Log.Logger.Debug("Received uninteresting package: {Package}", pkg);
                                    packages.Add(pkg);
                                }
                            }

                            break;
                        }
                        case "request":
                        {
                            ReadOnlyMemory<byte> ParseFrom(JsonElement element)
                            {
                                ReadOnlyMemory<byte> bytes = json.RootElement.GetProperty("payload")
                                    .GetProperty("compressed").GetBytesFromBase64();
                                using var inputStream = bytes.AsStream();
                                using var gzipDecoded = GzipHelpers.GzipDecompress(inputStream);
                                gzipDecoded.Seek(0, SeekOrigin.Begin);
                                return gzipDecoded.ToArray();
                            }

                            var request = new SpotifyRemoteMessage(
                                Type: SpotifyRemoteMessageType.Request,
                                Uri: string.Empty,
                                Payload: ParseFrom(json.RootElement)
                            );

                            var wasInteresting = false;
                            foreach (var callback in connectionInfo.Callbacks)
                            {
                                if (callback.PackageReceiveCondition(request))
                                {
                                    callback.Incoming(request);
                                    wasInteresting = true;
                                }
                            }

                            if (!wasInteresting)
                            {
                                Log.Logger.Debug("Received uninteresting request: {Request}", request);
                                //send reply
                                var key = json.RootElement.GetProperty("key").GetString()!;
                                var datareply = new
                                {
                                    type = "reply",
                                    key = key,
                                    payload = new
                                    {
                                        success = "false"
                                    }
                                };

                                ReadOnlyMemory<byte> jsonreply = JsonSerializer.SerializeToUtf8Bytes(datareply);
                                await mainChannel.Writer.WriteAsync(jsonreply);
                            }
                            else
                            {
                                //send reply
                                var key = json.RootElement.GetProperty("key").GetString()!;
                                var datareply = new
                                {
                                    type = "reply",
                                    key = key,
                                    payload = new
                                    {
                                        success = "true"
                                    }
                                };

                                ReadOnlyMemory<byte> jsonreply = JsonSerializer.SerializeToUtf8Bytes(datareply);
                                await mainChannel.Writer.WriteAsync(jsonreply);
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (lockObj)
                {
                    if (!alreadyReconnected)
                    {
                        alreadyReconnected = true;
                        CloseConnection(connectionId);
                        onConnectionLost(e);
                        pingPong?.Dispose();
                    }
                }
            }
        });
    }

    private static void CloseConnection(Guid connectionId)
    {
        //close connection
        Connections[connectionId].Stream.Dispose();
        Connections[connectionId].Sender.TryComplete();

        //DO NOT COMPLETE THE CALLBACKS

        // Connections[connectionId].Callbacks.ForEach(x => x.Reader.Writer.TryComplete());
        // Connections[connectionId].Callbacks.Clear();
        // Connections.Remove(connectionId);
    }


    private static async Task<Cluster> Put(PutStateRequest request,
        string connectionId,
        string jwt,
        string deviceId,
        CancellationToken ct = default)
    {
        const string spclient = "gae2-spclient.spotify.com:443";

        var url = $"https://{spclient}/connect-state/v1/devices/{deviceId}";
        var bearerHeader = new AuthenticationHeaderValue("Bearer", jwt);

        var headers = new Dictionary<string, string>
        {
            { "X-Spotify-Connection-Id", connectionId },
            { "accept", "gzip" }
        };

        using var body = GzipHelpers.GzipCompress(request.ToByteArray().AsMemory());
        using var response = await HttpIO.Put(url, bearerHeader, headers, body, ct);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var gzip = GzipHelpers.GzipDecompress(responseStream);
        gzip.Position = 0;
        var cluster = Cluster.Parser.ParseFrom(gzip);
        return cluster;
    }


    public static async Task<Cluster> Create(
        string deviceId,
        Guid connectionId,
        Func<CancellationToken, ValueTask<string>> accessToken,
        SpotifyConfig config)
    {
        async Task<Cluster> CreateConnectionRecursively()
        {
            const string dealer = "gae2-dealer.spotify.com:443";
            var token = await accessToken(CancellationToken.None);
            var wss = $"wss://{dealer}?access_token={token}";
            var clws = await WebsocketIO.Connect(wss, CancellationToken.None);
            //read first message (which should be the remote connection id)
            var firstMessage = await WebsocketIO.Receive(clws, CancellationToken.None);
            using var json = JsonDocument.Parse(firstMessage);
            var headers = new Dictionary<string, string>();
            if (json.RootElement.TryGetProperty("headers", out var headersElement))
            {
                using var enumerator = headersElement.EnumerateObject();
                // headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
                foreach (var curr in enumerator)
                {
                    headers.Add(curr.Name, curr.Value.GetString());
                }
            }

            if (!headers.TryGetValue("Spotify-Connection-Id", out var remoteConnectionId))
            {
                await clws.CloseAsync(WebSocketCloseStatus.ProtocolError, "No remote connection id found",
                    CancellationToken.None);
                throw new Exception("No remote connection id found");
            }

            //Initiate handshake
            var newState = SpotifyLocalPlaybackState.Empty(config.Remote, deviceId);
            var cluster = await Put(
                newState.BuildPutStateRequest(PutStateReason.NewDevice, Option<TimeSpan>.None),
                remoteConnectionId,
                token,
                deviceId,
                CancellationToken.None);


            SetupConnectionListener(clws, cluster, remoteConnectionId, connectionId, async exception =>
            {
                Log.Logger.Error(exception, "Connection lost. Reconnecting...");
                //call self
                while (true)
                {
                    try
                    {
                        await Task.Delay(4000, CancellationToken.None);
                        Log.Logger.Debug("Reconnecting...");
                        await CreateConnectionRecursively();
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error(e, "Failed to reconnect. Retrying in 4 seconds...");
                    }
                }
            });

            return cluster;
        }

        var cluster = await CreateConnectionRecursively();

        return cluster;
    }

    public static Option<Cluster> GetInitialRemoteState(Guid mainConnectionId)
    {
        if (Connections.TryGetValue(mainConnectionId, out var connectionInfo))
        {
            if (connectionInfo.LatestCluster is not null)
                return connectionInfo.LatestCluster;
        }

        return Option<Cluster>.None;
    }
}

internal readonly record struct SpotifyRemoteMessageCallback(ChannelReader<SpotifyRemoteMessage> Reader, Action onDone);

internal delegate bool RemoteMessageReceiveCondition(SpotifyRemoteMessage packageToCheck);

internal readonly record struct SpotifyRemoteMessage(SpotifyRemoteMessageType Type, string Uri,
    ReadOnlyMemory<byte> Payload);

internal enum SpotifyRemoteMessageType
{
    Message,
    Request
}