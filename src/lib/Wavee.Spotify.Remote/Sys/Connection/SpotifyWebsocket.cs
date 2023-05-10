using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Remote.Helpers;
using Wavee.Spotify.Sys;

namespace Wavee.Spotify.Remote.Sys.Connection;

internal static class SpotifyWebsocket<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>
{
    /// <summary>
    /// Establishes a connection to the Spotify Websocket and returns the connection id.
    /// </summary>
    /// <param name="getBearerFunc">
    /// A function that returns a bearer token. This function will be called every time the connection is lost.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the connection attempt.
    /// </param>
    /// <returns></returns>
    public static Aff<RT, (WebSocket Socket, string ConnectionId)> EstablishConnection(
        Func<ValueTask<string>> getBearerFunc,
        CancellationToken cancellationToken = default) =>
        from dealer in AP<RT>.FetchDealer().Map(x => $"wss://{x.Host}:{x.Port}?access_token={{0}}")
        from bearer in getBearerFunc().ToAff()
        from websocket in Ws<RT>.Connect(string.Format(dealer, bearer), cancellationToken)
        from connectionId in ReadNextMessage(websocket, cancellationToken)
            .Map(msg =>
            {
                return msg.Headers
                    .Find("Spotify-Connection-Id")
                    .Match(
                        Some: x => x,
                        None: () => throw new Exception("Connection id not found in websocket message"));
            })
        select (websocket, connectionId);

    /// <summary>
    /// Reads the next message from the websocket.
    /// </summary>
    /// <param name="websocket">
    /// The websocket to read from.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// A message from the websocket.
    /// </returns>
    public static Aff<RT, SpotifyWebsocketMessage>
        ReadNextMessage(WebSocket websocket, CancellationToken ct = default) =>
        from message in Ws<RT>.Read(websocket)
        select SpotifyWebsocketMessage.ParseFrom(message);
}

internal readonly record struct SpotifyWebsocketMessage(HashMap<string, string> Headers,
    SpotifyWebsocketMessageType Type,
    string Uri,
    Option<ReadOnlyMemory<byte>> Payload)
{
    public static SpotifyWebsocketMessage ParseFrom(ReadOnlyMemory<byte> message)
    {
        using var json = JsonDocument.Parse(message);
        HashMap<string, string> headers = LanguageExt.HashMap<string, string>.Empty;
        if (json.RootElement.TryGetProperty("headers", out var headersElement))
        {
            using var enumerator = headersElement.EnumerateObject();
            headers =
                enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
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
            _ => throw new Exception("Unknown websocket message type")
        };

        var payload = type switch
        {
            SpotifyWebsocketMessageType.Request => Some(ReadFromRequest(json.RootElement.GetProperty("payload").GetProperty("compressed").GetBytesFromBase64())),
            SpotifyWebsocketMessageType.Message => Some(ReadFromMessage(headers,
                json.RootElement.GetProperty("payloads"))),
            _ => None
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

    private static ReadOnlyMemory<byte> ReadFromMessage(HashMap<string, string> headers, JsonElement getProperty)
    {
        if (headers.ContainsKey("Transfer-Encoding") ||
            (headers.Find("Content-Type").Match(
                Some: x => x.StartsWith("application/octet-stream"),
                None: () => false)))
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
            throw new Exception("Unknown websocket message content type");
        }
    }
}

internal enum SpotifyWebsocketMessageType
{
    ConnectionId,
    Message,
    Request
}