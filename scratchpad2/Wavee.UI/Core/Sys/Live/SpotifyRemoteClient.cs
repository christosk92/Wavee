using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
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
        throw new NotImplementedException();
    }

    public Task<Unit> Pause(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Unit> SetShuffle(bool isShuffling, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Unit> SetRepeat(RepeatState next, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Unit> SkipNext(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Unit> SkipPrevious(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Unit> SeekTo(TimeSpan to, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public IObservable<SpotifyRemoteState> ObserveRemoteState() => _client.Remote.StateUpdates
        .Where(c => c.IsSome)
        .Select(c => c.ValueUnsafe());
}