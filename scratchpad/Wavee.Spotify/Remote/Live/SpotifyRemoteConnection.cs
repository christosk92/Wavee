using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Infrastructure;
using Wavee.Spotify.Helpers;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Mercury;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Remote.Live;

internal sealed class SpotifyRemoteConnection : IDisposable
{
    private readonly ClientWebSocket _socket;
    private readonly Timer _keepAliveTimer;
    private readonly Subject<Cluster> _clusterSubject = new();
    private readonly TaskCompletionSource<Exception?> _tcs = new();

    private Cluster _latestCluster;

    public SpotifyRemoteConnection(ClientWebSocket socket, Cluster cluster)
    {
        _socket = socket;
        _keepAliveTimer = new Timer(
            callback: KeepAliveCallback,
            null,
            dueTime: Timeout.Infinite,
            period: Timeout.Infinite
        );
        _keepAliveTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
        _latestCluster = cluster;

        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                try
                {
                    var message = await ReadNextMessage(_socket, CancellationToken.None);
                    switch (message.Type)
                    {
                        case SpotifyWebsocketMessageType.Message:
                            HandleMessage(message);
                            break;
                        case SpotifyWebsocketMessageType.Pong:
                            Debug.WriteLine("Pong");
                            break;
                    }
                }
                catch (Exception x)
                {
                    _tcs.TrySetResult(x);
                    break;
                }
            }
        });
    }

    public IObservable<Cluster> Cluster => _clusterSubject.StartWith(_latestCluster);
    public bool IsClosed => _socket.State > WebSocketState.Open;
    public Task<Exception?> Closed => _tcs.Task;

    private void HandleMessage(SpotifyWebsocketMessage message)
    {
        if (message.Uri.StartsWith("hm://connect-state/v1/cluster"))
        {
            var clusterUpdate = ClusterUpdate.Parser.ParseFrom(message.Payload.Value.Span);
            _latestCluster = clusterUpdate.Cluster;
            _clusterSubject.OnNext(clusterUpdate.Cluster);
            //atomic(() => State.Swap(_ => SpotifyRemoteState.From(clusterUpdate.Cluster, _deviceId)));
            GC.Collect();
        }
        else if (message.Uri.StartsWith($"hm://playlist/v2/user/") && message.Uri.EndsWith("rootlist"))
        {
            //   atomic(() => _rootlistNotifSubj.OnNext(new SpotifyRootlistUpdateNotification(_userId)));
        }
        else if (message.Uri.StartsWith("hm://collection/") && message.Uri.EndsWith("/json"))
        {
            // var payload = message.Payload.ValueUnsafe();
            // using var jsonDoc = JsonDocument.Parse(payload);
            // using var rootArr = jsonDoc.RootElement.EnumerateArray();
            // foreach (var rootItemStr in rootArr.Select(c => c.ToString()))
            // {
            //     using var rootItem = JsonDocument.Parse(rootItemStr);
            //     using var items = rootItem.RootElement.GetProperty("items").EnumerateArray();
            //     foreach (var item in items)
            //     {
            //         var type = item.GetProperty("type").GetString();
            //         var removed = item.GetProperty("removed").GetBoolean();
            //         var addedAt = item.GetProperty("addedAt").GetUInt64();
            //         var result = new SpotifyLibraryUpdateNotification(
            //             Item: AudioId.FromBase62(
            //                 base62: item.GetProperty("identifier").GetString(),
            //                 itemType: type switch
            //                 {
            //                     "track" => AudioItemType.Track,
            //                     "artist" => AudioItemType.Artist,
            //                     "album" => AudioItemType.Album
            //                 }, ServiceType.Spotify),
            //             Removed: removed,
            //             AddedAt: removed ? Option<DateTimeOffset>.None : DateTimeOffset.FromUnixTimeSeconds((long)addedAt)
            //         );
            //         atomic(() => _libraryNotifSubj.OnNext(result));
            //     }
            // }
        }
    }

    private async void KeepAliveCallback(object? state)
    {
        var message = new
        {
            type = "ping"
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        await _socket.SendAsync(json, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public void Dispose()
    {
        _socket.Dispose();
        _keepAliveTimer.Dispose();
    }


    public static async Task<SpotifyRemoteConnection> ConnectAsync(string dealer,
        Func<IMercuryClient> mercuryFactory,
        SpotifyConnection connection,
        CancellationToken cancellationToken = default)
    {
        var mercury = mercuryFactory();
        var token = await mercury.GetToken(cancellationToken);
        var url = $"wss://{dealer}/?access_token={token}";
        var ws = await WebsocketIO.Connect(url, ct: cancellationToken);

        //Read the first message, and then setup the loop
        var message = await ReadNextMessage(ws, CancellationToken.None);
        if (message.Type is not SpotifyWebsocketMessageType.ConnectionId)
        {
            await ws.CloseAsync(WebSocketCloseStatus.ProtocolError, "Expected ConnectionId",
                CancellationToken.None);
            throw new Exception("Expected ConnectionId");
        }


        var connectionId = message.Headers["Spotify-Connection-Id"];
        if (string.IsNullOrEmpty(connectionId))
        {
            await ws.CloseAsync(WebSocketCloseStatus.ProtocolError, "Expected ConnectionId",
                CancellationToken.None);
            throw new Exception("Expected ConnectionId");
        }

        //Send the handshake 
        var newState = SpotifyLocalPlaybackState.Empty(connection.Config.Remote, connection.DeviceId);
        var cluster = await Put(newState.BuildPutStateRequest(PutStateReason.NewDevice, null),
            connectionId,
            token,
            connection.DeviceId,
            CancellationToken.None);

        return new SpotifyRemoteConnection(ws, cluster);
    }

    private static async Task<Cluster> Put(PutStateRequest request,
        string connectionId,
        string jwt,
        string deviceId,
        CancellationToken ct = default)
    {
        var spClient = SpotifyConnectionAccessor.SpClient;
        var url = $"https://{spClient}/connect-state/v1/devices/{deviceId}";
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
        var cluster = Eum.Spotify.connectstate.Cluster.Parser.ParseFrom(gzip);
        return cluster;
    }

    private static async Task<SpotifyWebsocketMessage> ReadNextMessage(ClientWebSocket ws, CancellationToken ct)
    {
        var message = await WebsocketIO.Receive(ws, ct);
        return SpotifyWebsocketMessage.ParseFrom(message);
    }
}

internal enum SpotifyWebsocketMessageType
{
    ConnectionId,
    Message,
    Request,
    Pong
}

internal readonly record struct SpotifyWebsocketMessage(Dictionary<string, string> Headers,
    SpotifyWebsocketMessageType Type,
    string Uri,
    ReadOnlyMemory<byte>? Payload)
{
    public static SpotifyWebsocketMessage ParseFrom(ReadOnlyMemory<byte> message)
    {
        using var json = JsonDocument.Parse(message);
        var headers = new Dictionary<string, string>();
        if (json.RootElement.TryGetProperty("headers", out var headersElement))
        {
            using var enumerator = headersElement.EnumerateObject();
            //headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
            foreach (var header in enumerator)
            {
                headers.Add(header.Name, header.Value.GetString());
            }
        }

        var uri =
            json.RootElement.TryGetProperty("uri", out var uriProp)
                ? uriProp.GetString()
                : json.RootElement.TryGetProperty("key", out var keyProp)
                    ? keyProp.GetString()
                    : string.Empty;

        var type = json.RootElement.GetProperty("type").GetString() switch
        {
            "message" => uri.StartsWith("hm://pusher/v1/connections/")
                ? SpotifyWebsocketMessageType.ConnectionId
                : SpotifyWebsocketMessageType.Message,
            "request" => SpotifyWebsocketMessageType.Request,
            "pong" => SpotifyWebsocketMessageType.Pong,
            _ => throw new Exception("Unknown websocket message type")
        };

        var payload = type switch
        {
            SpotifyWebsocketMessageType.Request => ReadFromRequest(json.RootElement.GetProperty("payload")
                .GetProperty("compressed").GetBytesFromBase64()),
            SpotifyWebsocketMessageType.Message when json.RootElement.TryGetProperty("uri",
                                                         out var msgUri) &&
                                                     (msgUri.GetString().StartsWith("hm://playlist/v2/user/") ||
                                                      msgUri.GetString().StartsWith("hm://playlist/user/"))
                => ReadFromRootlistUpdate(json.RootElement.GetProperty("payloads")),
            SpotifyWebsocketMessageType.Message => ReadFromMessage(headers,
                json.RootElement.GetProperty("payloads")),
            _ => null
        };

        return new SpotifyWebsocketMessage(headers, type, uri, payload);
    }

    private static ReadOnlyMemory<byte> ReadFromRequest(ReadOnlyMemory<byte> bytes)
    {
        using var inputStream = bytes.AsStream();
        using var gzipDecoded = GzipHelpers.GzipDecompress(inputStream);
        gzipDecoded.Seek(0, SeekOrigin.Begin);
        return gzipDecoded.ToArray();
    }


    private static ReadOnlyMemory<byte> ReadFromRootlistUpdate(JsonElement getProperty)
    {
        using var enumerator = getProperty.EnumerateArray();
        using var buffer = new MemoryStream();
        foreach (var element in enumerator)
        {
            ReadOnlySpan<byte> bytes = element.GetBytesFromBase64();
            buffer.Write(bytes);
        }

        buffer.Flush();
        buffer.Seek(0, SeekOrigin.Begin);
        // if (headers.ContainsKey("Transfer-Encoding") && headers["Transfer-Encoding"] is "gzip")
        // {
        //     using var gzipDecoded = GzipHelpers.GzipDecompress(buffer);
        //     gzipDecoded.Seek(0, SeekOrigin.Begin);
        //     return gzipDecoded.ToArray();
        // }
        return buffer.ToArray();
    }

    private static ReadOnlyMemory<byte> ReadFromMessage(Dictionary<string, string> headers, JsonElement getProperty)
    {
        if (headers.ContainsKey("Transfer-Encoding") ||
            headers.ContainsKey("Content-Type") && headers["Content-Type"] is "application/octet-stream")
        {
            using var enumerator = getProperty.EnumerateArray();
            using var buffer = new MemoryStream();
            foreach (var element in enumerator)
            {
                ReadOnlySpan<byte> bytes = element.GetBytesFromBase64();
                buffer.Write(bytes);
            }

            buffer.Flush();
            buffer.Seek(0, SeekOrigin.Begin);
            if (headers.ContainsKey("Transfer-Encoding") && headers["Transfer-Encoding"] is "gzip")
            {
                using var gzipDecoded = GzipHelpers.GzipDecompress(buffer);
                gzipDecoded.Seek(0, SeekOrigin.Begin);
                return gzipDecoded.ToArray();
            }

            return buffer.ToArray();
        }
        else if (headers.ContainsKey("Content-Type") && headers["Content-Type"] is "application/json")
        {
            return Encoding.UTF8.GetBytes(getProperty.GetRawText());
        }
        else if (headers.ContainsKey("Content-Type") && headers["Content-Type"] is "text/plain")
        {
            return Encoding.UTF8.GetBytes(getProperty.GetRawText());
        }
        else
        {
            return Encoding.UTF8.GetBytes(getProperty.GetRawText());
        }
    }
}