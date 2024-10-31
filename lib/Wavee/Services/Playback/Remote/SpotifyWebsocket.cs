using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Wavee.Helpers;
using Wavee.Interfaces;
using Wavee.Models.Remote;

namespace Wavee.Services.Playback.Remote;

internal sealed partial class SpotifyWebsocket : ISpotifyWebsocket
{
    private readonly ILogger<SpotifyWebsocket> _logger;
    private readonly string _url;
    private ClientWebSocket _ws;
    private readonly BehaviorSubject<SpotifyWebsocketMessage?> _messageSubject = new(null);
    private readonly CancellationTokenSource _masterCts = new();
    private readonly AsyncAutoResetEvent _pingPongEvent = new(false);
    private readonly AsyncManualResetEvent _connectionEvent = new(false);

    public SpotifyWebsocket(string url, ILogger<SpotifyWebsocket> logger)
    {
        _logger = logger;
        var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        _url = url;
        _ws = ws;

        Task.Run(Runner);
        Task.Run(PingRunner);
    }

    private async Task PingRunner()
    {
        try
        {
            while (!_masterCts.IsCancellationRequested)
            {
                await _connectionEvent.WaitAsync(_masterCts.Token);

                // We send a ping then we wait for pong within 10 seconds
                await SendPingAsync(_masterCts.Token);

                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_masterCts.Token);
                combinedCts.CancelAfter(TimeSpan.FromSeconds(10));
                await _pingPongEvent.WaitAsync(combinedCts.Token);

                await Task.Delay(TimeSpan.FromSeconds(30), _masterCts.Token);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in websocket ping runner. We will abort the connection.");
            Dispose();
        }
    }

    private async Task SendPingAsync(CancellationToken cancellationToken)
    {
        var ping = new SendPing();
        var pingJson = JsonSerializer.SerializeToUtf8Bytes(ping, SpotifyClient.DefaultJsonOptions);
        await _ws.SendAsync(new ArraySegment<byte>(pingJson), WebSocketMessageType.Text,
            true, cancellationToken);
    }

    private async Task Runner()
    {
        try
        {
            while (!_masterCts.IsCancellationRequested)
            {
                await _connectionEvent.WaitAsync(_masterCts.Token);

                using var jsonDoc = await ReadActualMessage(_masterCts.Token);
                var messageType = jsonDoc.RootElement.GetProperty("type").GetString();
                switch (messageType)
                {
                    case "pong":
                        _logger.LogDebug("Received pong");
                        _pingPongEvent.Set();
                        break;
                    case "message":
                    {
                        var uri = jsonDoc.RootElement.GetProperty("uri").GetString();
                        var headers = jsonDoc.RootElement.GetProperty("headers").EnumerateObject()
                            .ToDictionary(x => x.Name, x => x.Value.GetString());
                        var payload = ReadPayload(headers, jsonDoc.RootElement.GetProperty("payloads"));
                        var message = new SpotifyWebsocketMessage
                        {
                            Type = SpotifyWebsocketMessageType.Message,
                            Uri = uri,
                            Payload = payload,
                            MessageId = Guid.NewGuid()
                        };
                        _messageSubject.OnNext(message);
                        break;
                    }
                    case "request":
                    {
                        var uri = jsonDoc.RootElement.GetProperty("message_ident").GetString();
                        var headers = jsonDoc.RootElement.GetProperty("headers").EnumerateObject()
                            .ToDictionary(x => x.Name, x => x.Value.GetString());
                        var payload = ReadPayload(headers,
                            jsonDoc.RootElement.GetProperty("payload").GetProperty("compressed"));
                        var key = jsonDoc.RootElement.GetProperty("key").GetGuid();
                        var message = new SpotifyWebsocketMessage
                        {
                            Type = SpotifyWebsocketMessageType.Request,
                            Uri = uri,
                            Payload = payload,
                            MessageId = key
                        };
                        _messageSubject.OnNext(message);
                        break;
                    }
                    default:
                        _logger.LogWarning($"Unknown message type received: {messageType}");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in websocket runner. Dispose the connection.");
        }

        Dispose();
        _masterCts.Dispose();
    }


    public bool Connected => _ws?.State is WebSocketState.Open
                             && !string.IsNullOrEmpty(ConnectionId);

    public string ConnectionId { get; private set; }

    public IObservable<SpotifyWebsocketMessage> Messages => _messageSubject
        .Skip(1) // Skip the initial value
        .Where(x => x is not null).Select(x => x!)
        .DistinctUntilChanged(x => x.MessageId);

    public async Task Reply(string key, bool success)
    {
        var reply = new SendRequestReply
        {
            key = key,
            type = "reply",
            payload = new SendRequestReply.SendRequestReplyPayload
            {
                success = success
            }
        };

        var replyJson = JsonSerializer.SerializeToUtf8Bytes(reply, SpotifyClient.DefaultJsonOptions);
        await _ws.SendAsync(new ArraySegment<byte>(replyJson), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }

    public IObservable<Unit> Disposed => _disposedSubj.Skip(1);


    public async Task ConnectAsync(CancellationToken token)
    {
        _logger.LogInformation("Connecting to Spotify Websocket");

        await _ws.ConnectAsync(new Uri(_url), token);
        using var connectionId = await ReadActualMessage(_masterCts.Token);
        const string CONNECTION_ID_PARAM = "connection_id";
        const string CONNECTION_ID_URL = $"hm://pusher/v1/connections/{{{CONNECTION_ID_PARAM}}}";
        var uri = connectionId.RootElement.GetProperty("uri").GetString();
        var decodedUri = Uri.UnescapeDataString(uri);
        var (regex, _) = SpotifyWebsocketRouter.CreateRegexFromPattern(CONNECTION_ID_URL);
        var match = regex.Match(decodedUri);
        ConnectionId = match.Groups[1].Value;
        _logger.LogInformation("Connected to Spotify Websocket with connection id: {ConnectionId}", ConnectionId);
        _connectionEvent.Set();
    }

    private byte[] ReadPayload(Dictionary<string, string> headers, JsonElement payloads)
    {
        // not everything is gzip compressed
        // it is if the content-encoding header is present and set to gzip
        if (!headers.TryGetValue("Transfer-Encoding", out var contentEncoding) || contentEncoding != "gzip")
        {
            var contentType = headers.GetValueOrDefault("Content-Type");
            return ReadRegularPayload(payloads, contentType);
            return null;
        }

        byte[] decompressedBytes = GzipDecompression.DecodeAndDecompressBase64Gzip(payloads);
        return decompressedBytes;
    }

    private byte[] ReadRegularPayload(JsonElement payloads, string contentType)
    {
        //throw new NotImplementedException()
        switch (payloads.ValueKind)
        {
            case JsonValueKind.Array:
            {
                //ValueKind = Array : "["ChYIABIQbpCJVAsES464ZnNK9xPUdjAB"]"
                StringBuilder sb = new();
                foreach (var payload in payloads.EnumerateArray())
                {
                    if (payload.ValueKind is JsonValueKind.Object)
                    {
                        sb.Append(payload.GetRawText());
                    }
                    else
                    {
                        sb.Append(payload.GetString());
                    }
                }

                switch (contentType)
                {
                    case "text/plain":
                    case "application/json":
                        return Encoding.UTF8.GetBytes(sb.ToString());
                    case "application/octet-stream":
                    {
                        var bytes = Convert.FromBase64String(sb.ToString());
                        return bytes;
                    }
                    default:
                    {
                        // just try bytes
                        var bytes = Convert.FromBase64String(sb.ToString());
                        return bytes;
                    }
                }

                break;
            }
            case JsonValueKind.String:
            {
                break;
            }
        }

        return null;
    }

    private async Task<JsonDocument> ReadActualMessage(CancellationToken cancellationToken)
    {
        WebSocketReceiveResult result;

        var buffer = new ArraySegment<byte>(new byte[1024]);
        using var ms = new MemoryStream();
        do
        {
            result = await _ws.ReceiveAsync(buffer, _masterCts.Token);
            Debug.Assert(buffer.Array != null, "buffer.Array != null");
            ms.Write(buffer.Array, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);

        var jsonDoc = await JsonDocument.ParseAsync(ms, cancellationToken: cancellationToken);
        return jsonDoc;
    }

    private bool _disposed;
    private readonly object _disposeLock = new();
    private readonly BehaviorSubject<Unit> _disposedSubj = new(Unit.Default);

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _messageSubject.OnCompleted();
            _messageSubject.Dispose();
            _masterCts?.Cancel();
            _ws?.Dispose();

            ConnectionId = null;
            _ws = null;
            _disposedSubj.OnNext(Unit.Default);
        }
    }
}