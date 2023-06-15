using System.Reactive.Linq;
using Eum.Spotify.context;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Core.Contracts.Playback;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyRemoteClient : IRemotePlaybackClient
{
    private readonly SpotifyClient _client;
    public SpotifyRemoteClient(SpotifyClient client)
    {
        _client = client;
    }

    public Task<Unit> Resume(CancellationToken ct = default)
    {
        return _client.Remote.Resume(ct);
    }

    public Task<Unit> Pause(CancellationToken ct = default)
    {
        return _client.Remote.Pause(ct);
    }

    public Task<Unit> SetShuffle(bool isShuffling, CancellationToken ct = default)
    {
        return _client.Remote.SetShuffle(isShuffling, ct);
    }

    public Task<Unit> SetRepeat(RepeatState next, CancellationToken ct = default)
    {
        return _client.Remote.SetRepeat(next, ct);
    }

    public Task<Unit> SkipNext(CancellationToken ct = default)
    {
        return _client.Remote.SkipNext(ct);
    }

    public Task<Unit> SkipPrevious(CancellationToken ct = default)
    {
        return _client.Remote.SkipPrevious(ct);
    }

    public Task<Unit> SeekTo(TimeSpan to, CancellationToken ct = default)
    {
        return _client.Remote.SeekTo(to, ct);
    }

    public Task<Unit> PlayContextPaged(string contextId, IEnumerable<ContextPage> pages, int trackIndex, int pageIndex, HashMap<string, string> metadata)
    {
        return _client.Remote.PlayContextPaged(contextId, pages, trackIndex, pageIndex, metadata);
    }

    public Task<Unit> PlayContextRaw(string contextId, string contextUrl, int trackIndex, Option<AudioId> trackId, int pageIndex,
        HashMap<string, string> metadata)
    {
        return _client.Remote.PlayContextRaw(contextId, contextUrl, trackIndex, trackId, pageIndex, metadata);
    }

    public IObservable<SpotifyRemoteState> ObserveRemoteState() => _client.Remote.StateUpdates
        .Where(c => c.IsSome)
        .Select(c => c.ValueUnsafe());
}