using System.Net.WebSockets;

namespace Wavee.Infrastructure.IO;

public static class WebsocketIO
{
    public static async Task<ClientWebSocket> Connect(string url, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        await ws.ConnectAsync(new Uri(url), ct);
        return ws;
    }

    public static async Task<ReadOnlyMemory<byte>> Receive(ClientWebSocket ws, CancellationToken ct = default)
    {
        WebSocketReceiveResult result;

        var buffer = new ArraySegment<byte>(new byte[1024]);
        using var ms = new MemoryStream();
        do
        {
            result = await ws.ReceiveAsync(buffer, ct);
            ms.Write(buffer.Array, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }
}