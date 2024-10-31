using System.Reactive;
using Eum.Spotify.connectstate;
using Wavee.Services.Playback;
using Wavee.Services.Playback.Remote;

namespace Wavee.Interfaces;

internal interface ISpotifyWebsocketState
{
    IDisposable AddMessageHandler(string path, SpotifyWebsocketRouter.MessageHandler handler);
    void AddRequestHandler(string path, SpotifyWebsocketRouter.RequestHandler handler);
    IObservable<SpotifyRemotePlaybackState?> PlaybackState { get; }
    string? ConnectionId { get; }
    internal IObservable<Unit> Reconnected { get;}
    internal IObservable<bool> Connected { get; }
    ValueTask<Device?> ConnectAsync(string deviceName, DeviceType deviceType, CancellationToken cancellationToken = default);
    Task ForceNewClusterUpdate(Cluster newRemoteState);
    
    Device NewState();
    void NewPutStateRequest(PutStateRequest putState);
    Task<Unit> RegisterAckId(string ackid);
}