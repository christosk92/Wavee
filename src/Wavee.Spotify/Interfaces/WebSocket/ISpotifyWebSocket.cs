using System.Net.WebSockets;

namespace Wavee.Spotify.Interfaces.WebSocket;

public interface ISpotifyWebSocket : IDisposable
{
    WebSocketState State { get; }
    bool Connected { get;  }
    Task ConnectAsync(string url, CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> segment, CancellationToken cancellationToken);
    Task SendAsync(string reply, CancellationToken none);
}