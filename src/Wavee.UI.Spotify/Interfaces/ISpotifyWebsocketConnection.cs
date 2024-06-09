using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;

namespace Wavee.UI.Spotify.Interfaces;

internal interface ISpotifyWebsocketConnection : IDisposable
{
    Task<string> Connect(CancellationToken cancellationToken);
    bool Connected { get; }
    public event EventHandler<(Exception?, WebSocketCloseStatus?)>? Disconnected;
}