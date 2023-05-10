using Eum.Spotify.connectstate;
using Wavee.Spotify.Clients.Playback;

namespace Wavee.Spotify.Clients.Remote;

public interface IRemoteClient
{
    /// <summary>
    /// Connects to the remote client and returns an observable of the playback state.
    /// </summary>
    /// 
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the connect operation.
    /// </param>
    /// 
    /// <returns>
    /// An observable of the playback state. 
    /// <para>
    /// If <see cref="SpotifyPlaybackState.IsPlayingOnRemote"/> is false, it means that the playback is not controlled by the remote client
    /// and rather by the local client (this device.)
    /// </para>
    /// <para>
    /// In that case all playback events are emitted by the appropriate <see cref="IPlaybackClient"/>.
    /// </para>
    /// </returns>
    Task<IObservable<SpotifyPlaybackState>> Connect(CancellationToken ct = default);
}

public struct SpotifyPlaybackState
{
    public required bool IsPlayingOnRemote { get; init; }
    public required Option<Cluster> RemoteState { get; init; }
}