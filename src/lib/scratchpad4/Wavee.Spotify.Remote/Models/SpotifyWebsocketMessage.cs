using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using LanguageExt;
using Wavee.Spotify.Remote.Helpers;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Remote.Models;

public readonly record struct SpotifyWebsocketMessage(HashMap<string, string> Headers,
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
            headers = enumerator.Fold(headers, (acc, curr) => acc.Add(curr.Name, curr.Value.GetString()));
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
            SpotifyWebsocketMessageType.Request => Some(ReadFromRequest(json.RootElement.GetProperty("payload")
                .GetProperty("compressed").GetBytesFromBase64())),
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

public enum SpotifyWebsocketMessageType
{
    ConnectionId,
    Message,
    Request
}