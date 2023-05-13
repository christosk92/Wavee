using System.Net.WebSockets;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Live;

internal readonly struct WebsocketIOImpl : WebsocketIO
{
    public async ValueTask<WebSocket> Connect(string url, CancellationToken ct = default)
    {
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        await socket.ConnectAsync(new Uri(url), ct);
        return socket;
    }

    public async ValueTask<ReadOnlyMemory<byte>> Receive(WebSocket socket, CancellationToken ct = default)
    {
        WebSocketReceiveResult result;

        var buffer = new ArraySegment<byte>(new byte[1024]);
        using var ms = new MemoryStream();
        do
        {
            result = await socket.ReceiveAsync(buffer, ct);
            ms.Write(buffer.Array, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }

    public async ValueTask<Unit> Write(WebSocket socket, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        await socket.SendAsync(data, WebSocketMessageType.Text, true, ct);
        return unit;
    }

    public static WebsocketIO Default { get; } = new WebsocketIOImpl();
}