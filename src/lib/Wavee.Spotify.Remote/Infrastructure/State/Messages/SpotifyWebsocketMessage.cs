using System.IO.Compression;
using System.Text.Json;

namespace Wavee.Spotify.Remote.Infrastructure.State.Messages;

internal readonly record struct SpotifyWebsocketMessage(SpotifyWebsocketMessageType Type,
    string Uri,
    ReadOnlyMemory<byte> Payload)
{
    public static SpotifyWebsocketMessage ParseFrom(ReadOnlyMemory<byte> message)
    {
        using var reader = JsonDocument.Parse(message);
        var type = reader.RootElement.GetProperty("type").GetString();
        return type switch
        {
            "message" => FromMessage(reader.RootElement),
            "request" => FromRequest(reader.RootElement),
            _ => throw new InvalidOperationException()
        };
    }

    private static SpotifyWebsocketMessage FromMessage(JsonElement element)
    {
        var uri = element.GetProperty("uri").GetString();
        if (uri.StartsWith("hm://connect-state/v1/cluster"))
        {
            using var payloads = element.GetProperty("payloads").EnumerateArray();
            var payload = payloads.First();
            var decoded = DecodeGzip(payload.GetBytesFromBase64());
            return new SpotifyWebsocketMessage(
                Type: SpotifyWebsocketMessageType.Message,
                Uri: uri,
                Payload: decoded
            );
        }

        //else idk
        return default;
    }

    private static SpotifyWebsocketMessage FromRequest(JsonElement element)
    {
        var key = element.GetProperty("key").GetString();
        ReadOnlyMemory<byte> payload = DecodeGzip(element.GetProperty("payload")
            .GetProperty("compressed")
            .GetBytesFromBase64());

        return new SpotifyWebsocketMessage(
            Type: SpotifyWebsocketMessageType.Request,
            Uri: key,
            Payload: payload);
    }

    private static ReadOnlyMemory<byte> DecodeGzip(ReadOnlySpan<byte> payload)
    {
        using var output = new MemoryStream();
        using (var stream = new MemoryStream())
        {
            stream.Write(payload);
            stream.Seek(0, SeekOrigin.Begin);
            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                Span<byte> buffer = new byte[1024];
                int nRead;
                while ((nRead = gzipStream.Read(buffer)) > 0)
                {
                    output.Write(buffer.Slice(0, nRead));
                }
            }
        }

        output.Position = 0;
        output.Seek(0, SeekOrigin.Begin);
        return output.ToArray();
    }

    public static SpotifyRequestMessage ParseRequest(ReadOnlyMemory<byte> payload)
    {
        using var reader = JsonDocument.Parse(payload);

        var messageId = reader.RootElement.GetProperty("message_id").GetUInt32();
        var sentByDeviceId = reader.RootElement.GetProperty("sent_by_device_id").GetString();

        var command = reader.RootElement.GetProperty("command");
        var endpointStr = command.GetProperty("endpoint").GetString();
        var endpoint = endpointStr switch
        {
            "transfer" => RequestCommandEndpointType.Transfer,
            _ => throw new InvalidOperationException()
        };

        ReadOnlyMemory<byte> commandPayload = command.GetProperty("data").GetBytesFromBase64();
        return new SpotifyRequestMessage(
            MessageId: messageId,
            SentByDeviceId: sentByDeviceId,
            CommandEndpoint: endpoint,
            CommandPayload: commandPayload
        );
    }
}

internal readonly record struct SpotifyRequestMessage(
    uint MessageId,
    string SentByDeviceId,
    RequestCommandEndpointType CommandEndpoint,
    ReadOnlyMemory<byte> CommandPayload);

internal enum RequestCommandEndpointType
{
    Transfer
}

internal enum SpotifyWebsocketMessageType
{
    Message,
    Request
}