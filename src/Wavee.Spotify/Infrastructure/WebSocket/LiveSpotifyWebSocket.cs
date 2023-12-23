using System.Net.WebSockets;
using System.Text;
using Wavee.Spotify.Core.Interfaces.WebSocket;

namespace Wavee.Spotify.Infrastructure.WebSocket;

internal sealed class LiveSpotifyWebSocket : ISpotifyWebSocket
{
    private ClientWebSocket? _client;


    public void Dispose()
    {
        // TODO release managed resources here
    }

    public WebSocketState State => _client?.State ?? WebSocketState.None;
    public bool Connected => _client?.State is WebSocketState.Open;

    public async Task ConnectAsync(string url, CancellationToken cancellationToken)
    {
        ClientWebSocket clientWebSocket = new();
        clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        clientWebSocket.Options.SetRequestHeader("Origin",
            "https://open.spotify.com");
        await clientWebSocket.ConnectAsync(new Uri(url), cancellationToken);
        _client = clientWebSocket;
    }

    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> segment, CancellationToken cancellationToken)
    {
        return _client!.ReceiveAsync(segment, cancellationToken);
    }

    public Task SendAsync(string reply, CancellationToken none)
    {
        return _client!.SendAsync(Encoding.UTF8.GetBytes(reply), WebSocketMessageType.Text, true, none);
    }
}