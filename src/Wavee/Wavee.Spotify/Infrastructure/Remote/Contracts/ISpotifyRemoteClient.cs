using Eum.Spotify.context;
using Eum.Spotify.playlist4;
using LanguageExt;
using LanguageExt.UnitsOfMeasure;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback.Contracts;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public interface ISpotifyRemoteClient
{
    IObservable<Option<SpotifyRemoteState>> StateUpdates { get; }
    IObservable<SpotifyRootlistUpdateNotification> RootlistChanged { get; }
    IObservable<SpotifyLibraryUpdateNotification> LibraryChanged { get; }
    Task<Option<Unit>> Takeover(CancellationToken ct = default);
    Task<Unit> SetShuffle(bool isShuffling, CancellationToken ct = default);
    Task<Unit> Resume(CancellationToken ct = default);
    Task<Unit> Pause(CancellationToken ct = default);
    Task<Unit> SkipNext(CancellationToken ct = default);
    Task<Unit> SkipPrevious(CancellationToken ct = default);
    Task<Unit> SetRepeat(RepeatState next, CancellationToken ct = default);
    Task<Unit> SeekTo(TimeSpan to, CancellationToken ct = default);
    Task<Unit> PlayContextPaged(string contextId, IEnumerable<ContextPage> pages, int trackIndex, int pageIndex, HashMap<string, string> metadata);
    Task<Unit> PlayContextRaw(string contextId, string contextUrl, int trackIndex, Option<AudioId> trackId, int pageIndex, HashMap<string, string> metadata);
    Task<Unit> RefreshState();
    IObservable<Diff> ObservePlaylist(AudioId id);
}