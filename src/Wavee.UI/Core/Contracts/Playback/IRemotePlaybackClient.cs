using Eum.Spotify.context;
using LanguageExt;
using Wavee.Core.Ids;
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
    Task<Unit> PlayContextPaged(string contextId, IEnumerable<ContextPage> pages, int trackIndex, int pageIndex, HashMap<string, string> metadata);
    Task<Unit> PlayContextRaw(string contextId, string contextUrl, int trackIndex, Option<AudioId> trackId, int pageIndex, HashMap<string, string> metadata);

    IObservable<SpotifyRemoteState> ObserveRemoteState();
 }