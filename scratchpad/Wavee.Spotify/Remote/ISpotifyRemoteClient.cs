namespace Wavee.Spotify.Remote;

public interface ISpotifyRemoteClient
{
    /// <summary>
    /// An observable that emits a value whenever the remote client is updated.
    /// Note that this observable will not emit a value until the client has been initialized.
    /// Also note that this observable will not emit a value if the client is playing on this device.
    /// For events then, use the playback observable.
    /// </summary>
    IObservable<SpotifyRemoteState> Updates { get; }

    /// <summary>
    /// The momentary state of the remote client.
    /// </summary>
    SpotifyRemoteState State { get; }
    
    Task Pause(CancellationToken cancellationToken = default);
    Task Resume(CancellationToken ct = default);
    Task SetShuffle(bool isShuffling, CancellationToken ct = default);
    Task SetRepeat(RepeatState next, CancellationToken ct = default);
    Task SkipNext(CancellationToken ct = default);
    Task SkipPrevious(CancellationToken ct = default);
    Task SeekTo(TimeSpan position, CancellationToken ct = default);
}