using System.Reactive;
using Wavee.Services.Playback.Remote;

namespace Wavee.Interfaces;

internal interface ISpotifyWebsocket : IDisposable
{
    bool Connected { get; }
    string ConnectionId { get; }
    IObservable<SpotifyWebsocketMessage> Messages { get; }
    Task Reply(string key, bool success);

    IObservable<Unit> Disposed { get; }
}