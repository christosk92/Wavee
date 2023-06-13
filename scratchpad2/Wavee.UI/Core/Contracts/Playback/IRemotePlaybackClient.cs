using LanguageExt;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.UI.Core.Contracts.Playback;

public interface IRemotePlaybackClient
{
    Task<Unit> Resume(CancellationToken ct = default);
    Task<Unit> Pause(CancellationToken ct = default);
    Task<Unit> SetShuffle(bool isShuffling, CancellationToken ct = default);
    Task<Unit> SetRepeat(RepeatState next, CancellationToken ct = default);
    Task<Unit> SkipNext(CancellationToken ct = default);
    Task<Unit> SkipPrevious(CancellationToken ct = default);
    Task<Unit> SeekTo(TimeSpan to, CancellationToken ct = default);
    IObservable<SpotifyRemoteState> ObserveRemoteState();
}