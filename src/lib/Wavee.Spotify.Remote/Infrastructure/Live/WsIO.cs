using System.Net.WebSockets;

namespace Wavee.Spotify.Remote.Infrastructure.Live;

internal readonly struct WsIO : Traits.WsIO
{
    private readonly ClientWebSocket _ws;

    public WsIO(ClientWebSocket ws)
    {
        _ws = ws;
    }

    public async ValueTask<Unit> Connect(string url, CancellationToken ct = default)
    {
        await _ws.ConnectAsync(new Uri(url), ct);
        return unit;
    }

    public async ValueTask<ReadOnlyMemory<byte>> Receive(CancellationToken ct = default)
    {
        WebSocketReceiveResult result;

        var buffer = new ArraySegment<byte>(new byte[1024]);
        using var ms = new MemoryStream();
        do
        {
            result = await _ws.ReceiveAsync(buffer, ct);
            ms.Write(buffer.Array, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);
        
        ms.Seek(0, SeekOrigin.Begin);
        return ms.ToArray();
    }
}