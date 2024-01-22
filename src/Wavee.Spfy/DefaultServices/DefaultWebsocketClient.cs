using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy.DefaultServices;

internal sealed partial class DefaultWebsocketClient : IWebsocketClient
{
    private bool _isDisposed;
    private readonly ClientWebSocket _clientWebSocket;

    public DefaultWebsocketClient()
    {
        ClientWebSocket clientWebSocket = new();
        clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        clientWebSocket.Options.SetRequestHeader("Origin",
            "https://open.spotify.com");

        _clientWebSocket = clientWebSocket;
    }

    public bool IsConnected => !_isDisposed && !string.IsNullOrEmpty(ConnectionId) &&
                               _clientWebSocket?.State is WebSocketState.Open;

    public string ConnectionId { get; set; }

    public Task Connect(string url, CancellationToken cancellationToken)
    {
        return _clientWebSocket.ConnectAsync(new Uri(url), cancellationToken);
    }

    public async Task<SpotifyWebsocketMessage> ReadNextMessage(CancellationToken cancellationToken)
    {
        using var message = new MemoryStream();
        bool endOfMessage = false;
        while (!endOfMessage)
        {
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);
            var result = await _clientWebSocket.ReceiveAsync(segment, cancellationToken: cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException(error: WebSocketError.ConnectionClosedPrematurely);
            }

            message.Write(buffer, 0, result.Count);
            endOfMessage = result.EndOfMessage;
        }

        message.Seek(0, SeekOrigin.Begin);
        using var jsondoc = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);
        var root = jsondoc.RootElement;
        var type = root.GetProperty("type").GetString();
        if (type is "pong")
            return new SpotifyWebsocketMessage(SpotifyWebsocketMessageType.Pong, Memory<byte>.Empty, "", null);
        var uri = type is "request"
            ? root.GetProperty("message_ident").GetString()
            : root.GetProperty("uri").GetString();

        var match = ConnIdRegex().Match(uri);
        if (match.Success)
        {
            var headers = root.GetProperty("headers");
            var connectionId = headers.GetProperty("Spotify-Connection-Id").GetString();
            ConnectionId = connectionId;
            return new SpotifyWebsocketMessage(SpotifyWebsocketMessageType.ConnectionId,
                Encoding.UTF8.GetBytes(connectionId), uri, null);
        }
        else
        {
            var headers = new Dictionary<string, string>();
            if (root.TryGetProperty("headers", out var headersElement))
            {
                using var enumerator = headersElement.EnumerateObject();
                // headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
                foreach (var curr in enumerator)
                {
                    headers.Add(curr.Name, curr.Value.GetString()!);
                }
            }


            var payload = ReadPayload(root, headers);

            var typeX = type switch
            {
                "message" => SpotifyWebsocketMessageType.Message,
                "request" => SpotifyWebsocketMessageType.Request,
            };
            return new SpotifyWebsocketMessage(typeX, payload, uri, typeX is SpotifyWebsocketMessageType.Request
                ? root.GetProperty("key").GetString()
                : "");
        }
    }

    public Task SendPing(CancellationToken ca)
    {
        //{"type":"ping"}	
        var ping = new { type = "ping" };
        var pingJson = JsonSerializer.SerializeToUtf8Bytes(ping);
        return _clientWebSocket.SendAsync(pingJson, WebSocketMessageType.Text, true, ca);
    }

    public Task SendJson(string reply, CancellationToken none)
    {
        var replyJson = Encoding.UTF8.GetBytes(reply);
        return _clientWebSocket.SendAsync(replyJson, WebSocketMessageType.Text, true, none);
    }

    private static ReadOnlyMemory<byte> ReadPayload(JsonElement messageRootElement, Dictionary<string, string> headers)
    {
        Memory<byte> payload = Memory<byte>.Empty;
        var gzip = false;
        var plainText = false;
        if (headers.TryGetValue("Transfer-Encoding", out var trnsfEncoding))
        {
            if (trnsfEncoding is "gzip")
            {
                gzip = true;
            }
        }

        if (headers.TryGetValue("Content-Type", out var cntEncoding))
        {
            if (cntEncoding is "text/plain")
            {
                plainText = true;
            }
        }

        if (messageRootElement.TryGetProperty("payloads", out var payloadsArr))
        {
            var payloads = new ReadOnlyMemory<byte>[payloadsArr.GetArrayLength()];
            for (var i = 0; i < payloads.Length; i++)
            {
                if (plainText)
                {
                    ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(payloadsArr[i].GetString());
                    payloads[i] = bytes;
                }
                else
                {
                    payloads[i] = payloadsArr[i].GetBytesFromBase64();
                }
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

    public void Dispose()
    {
        _isDisposed = true;
        _clientWebSocket.Dispose();
    }


    [GeneratedRegex(@"hm://pusher/v1/connections/([^/]+)")]
    private static partial Regex ConnIdRegex();
}