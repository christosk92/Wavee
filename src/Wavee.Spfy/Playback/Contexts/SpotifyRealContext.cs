using Eum.Spotify.context;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Contexting;
using Wavee.Spfy.Remote;

namespace Wavee.Spfy.Playback.Contexts;

internal abstract class SpotifyRealContext : ISpotifyContext
{
    protected readonly Guid _connectionId;
    private readonly Func<SpotifyId, CancellationToken, Task<WaveeStream>> _streamFactory;
    private ActiveSpotifyContextPage? _activePage;
    private protected readonly LinkedList<SpotifyContextPage> PagesCache = new();
    private Option<int> _restoreIndex = Option<int>.None;

    protected SpotifyRealContext(Guid connectionId,
        string contextUri,
        string contextUrl,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory)
    {
        _connectionId = connectionId;
        _streamFactory = streamFactory;
        ContextUri = contextUri;
        ContextUrl = contextUrl;
    }

    public string ContextUri { get; }
    public string ContextUrl { get; }
    public abstract HashMap<string, string> ContextMetadata { get; }

    public virtual ValueTask RefreshContext(Context ctx, bool clear)
    {
        int idx = -1;
        bool found = false;
        foreach (var page in PagesCache)
        {
            foreach (var track in page.Tracks)
            {
                idx++;
                if (track == _activePage?.CurrentTrack?.Value)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        if (found)
        {
            _restoreIndex = idx;
        }

        PagesCache.Clear();
        _activePage = null;

        return Restore();
    }

    private async ValueTask Restore()
    {
        if (_restoreIndex.IsNone)
        {
            return;
        }

        var idx = _restoreIndex.ValueUnsafe();
        while (!await MoveTo(idx) && idx >= 0)
        {
            idx--;
        }

        _restoreIndex = Option<int>.None;
    }

    protected abstract ValueTask<Option<SpotifyContextPage>> NextPage();

    protected abstract ValueTask<IReadOnlyCollection<ContextPage>> GetAllPages();

    protected async ValueTask FillAllPages()
    {
        var pages = await GetAllPages();
        var newCtx = new Context();
        newCtx.Uri = ContextUri;
        newCtx.Url = ContextUrl;
        foreach (var page in pages)
        {
            newCtx.Pages.Add(page);
        }

        foreach (var mt in ContextMetadata)
        {
            newCtx.Metadata.Add(mt.Key, mt.Value);
        }

        await RefreshContext(newCtx, false);
    }

    public async ValueTask<Option<WaveeContextStream>> GetNextStream()
    {
        await Restore();

        var nextTrack = await GetNextTrack();
        if (nextTrack.IsNone)
        {
            return Option<WaveeContextStream>.None;
        }

        var track = nextTrack.ValueUnsafe();
        var stream = await _streamFactory(track.Gid, CancellationToken.None);
        var ctxStream = new WaveeContextStream(stream,
            Common.ConstructComposedKeyForCurrentTrack(track, track.Gid));
        CurrentStream = Option<WaveeContextStream>.Some(ctxStream);
        return CurrentStream;
    }

    private async ValueTask<Option<SpotifyContextTrack>> GetNextTrack()
    {
        var activePage = _activePage;
        if (activePage is null)
        {
            // try to fetch the next page
            var nextPage = await NextPage();
            if (nextPage.IsNone)
            {
                return Option<SpotifyContextTrack>.None;
            }

            var newPage = PagesCache.AddLast(nextPage.ValueUnsafe());
            _activePage = new ActiveSpotifyContextPage(newPage);
            activePage = _activePage;
        }

        if (!activePage.TryMoveNext(out var track))
        {
            // try to fetch the next page
            var nextPage = await NextPage();
            if (nextPage.IsNone)
            {
                return Option<SpotifyContextTrack>.None;
            }

            var newPage = PagesCache.AddLast(nextPage.ValueUnsafe());
            _activePage = new ActiveSpotifyContextPage(newPage);
            return await GetNextTrack();
        }

        return track.Value;
    }

    public async ValueTask<Option<WaveeContextStream>> GetPreviousStream()
    {
        await Restore();

        if (_activePage is null)
        {
            return Option<WaveeContextStream>.None;
        }

        if (_activePage.CurrentTrack is null)
        {
            return Option<WaveeContextStream>.None;
        }

        var prevTrack = _activePage.CurrentTrack.Previous;
        if (prevTrack is null)
        {
            // try to fetch the previous page
            var prevPage = _activePage.CurrentPage.Previous;
            if (prevPage is null)
            {
                return Option<WaveeContextStream>.None;
            }

            _activePage = new ActiveSpotifyContextPage(prevPage);
            prevTrack = _activePage.CurrentTrack;
        }


        _activePage.CurrentTrack = prevTrack;
        var stream = await _streamFactory(prevTrack.Value.Gid, CancellationToken.None);
        var ctxStream = new WaveeContextStream(stream,
            Common.ConstructComposedKeyForCurrentTrack(prevTrack.Value, prevTrack.Value.Gid));
        CurrentStream = Option<WaveeContextStream>.Some(ctxStream);
        return CurrentStream;
    }

    public async ValueTask<Option<WaveeContextStream>> GetCurrentStream()
    {
        await Restore();

        if (_activePage is null)
        {
            return Option<WaveeContextStream>.None;
        }

        if (_activePage.CurrentTrack is null)
        {
            return Option<WaveeContextStream>.None;
        }

        var stream = await _streamFactory(_activePage.CurrentTrack.Value.Gid, CancellationToken.None);
        var ctxStream = new WaveeContextStream(stream,
            Common.ConstructComposedKeyForCurrentTrack(_activePage.CurrentTrack.Value,
                _activePage.CurrentTrack.Value.Gid));
        CurrentStream = Option<WaveeContextStream>.Some(ctxStream);
        return CurrentStream;
    }

    public Option<WaveeContextStream> CurrentStream { get; private set; }

    public async ValueTask<bool> MoveTo(int absoluteIndex)
    {
        // absolute index (0 -> 99999...)
        // we need to find the page that contains the track at the absolute index

        // We need to go back one because GetNextTrack will return the next track
        if (PagesCache.Count is 0)
        {
            var nextPage = await NextPage();
            if (nextPage.IsNone)
            {
                return false;
            }

            PagesCache.AddLast(nextPage.ValueUnsafe());
            return await MoveTo(absoluteIndex);
        }

        int seenTracks = 0;
        foreach (var pageCache in PagesCache)
        {
            if (seenTracks + pageCache.Tracks.Count > absoluteIndex)
            {
                // we have found the page
                _activePage = new ActiveSpotifyContextPage(PagesCache.Find(pageCache));
                var idx = absoluteIndex - seenTracks;
                _activePage.MoveTo(idx);
                return true;
            }

            seenTracks += pageCache.Tracks.Count;
        }

        // try next page
        var nextPageA = await NextPage();
        if (nextPageA.IsNone)
        {
            return false;
        }

        PagesCache.AddLast(nextPageA.ValueUnsafe());

        return await MoveTo(absoluteIndex);
    }

    public abstract ValueTask<bool> MoveToRandom();

    public async ValueTask<bool> TrySkip(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var nextTrack = await GetNextTrack();
            if (nextTrack.IsNone)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ActiveSpotifyContextPage
    {
        public ActiveSpotifyContextPage(LinkedListNode<SpotifyContextPage> currentPage)
        {
            CurrentPage = currentPage;
            CurrentTrack = null;
        }

        public bool IsDone { get; private set; }
        public LinkedListNode<SpotifyContextPage> CurrentPage { get; }
        public LinkedListNode<SpotifyContextTrack>? CurrentTrack { get; internal set; }

        public bool TryMoveNext(out SpotifyContextTrack? track)
        {
            if (CurrentTrack is null && !IsDone)
            {
                CurrentTrack = CurrentPage.Value.Tracks.First;
                track = CurrentTrack?.Value;
                return true;
            }
            else if (CurrentTrack is null && IsDone)
            {
                track = null;
                return false;
            }

            var nextTrack = CurrentTrack?.Next;
            if (nextTrack is null)
            {
                IsDone = true;
                track = null;
                return false;
            }

            track = nextTrack?.Value;
            CurrentTrack = nextTrack;

            return true;
        }

        public void MoveTo(int toIndex)
        {
            if (toIndex is 0)
            {
                CurrentTrack = CurrentPage.Value.Tracks.First;
                return;
            }

            var idx = toIndex;
            var iterated = 0;
            foreach (var track in CurrentPage.Value.Tracks)
            {
                if (iterated == idx)
                {
                    CurrentTrack = CurrentPage.Value.Tracks.Find(track);
                    return;
                }

                iterated++;
            }
        }
    }
}

internal sealed class SingularTrackContext : IWaveePlayerContext
{
    private Func<Task<WaveeStream>> _trackStreamFactory;

    public SingularTrackContext(Func<Task<WaveeStream>> trackStreamFactory)
    {
        _trackStreamFactory = trackStreamFactory;
    }

    public ValueTask<Option<WaveeContextStream>> GetNextStream()
    {
        if (_trackStreamFactory is null)
        {
            return new ValueTask<Option<WaveeContextStream>>(Option<WaveeContextStream>.None);
        }

        var stream = _trackStreamFactory();
        _trackStreamFactory = null;
        return new ValueTask<Option<WaveeContextStream>>(AwaitRes(stream));
    }


    public ValueTask<Option<WaveeContextStream>> GetPreviousStream()
    {
        return new ValueTask<Option<WaveeContextStream>>(Option<WaveeContextStream>.None);
    }

    public async ValueTask<Option<WaveeContextStream>> GetCurrentStream()
    {
        if (_trackStreamFactory is null)
        {
            return Option<WaveeContextStream>.None;
        }

        var stream = await _trackStreamFactory();
        var ctxStream = new WaveeContextStream(stream, new ComposedKey(stream.Metadata.Id!));
        CurrentStream = Option<WaveeContextStream>.Some(ctxStream);
        return CurrentStream;
    }

    public Option<WaveeContextStream> CurrentStream { get; private set; }

    public ValueTask<bool> TrySkip(int count)
    {
        // Cant skip
        return new ValueTask<bool>(false);
    }

    public ValueTask<bool> TryPeek(int count)
    {
        // Cant peek
        return new ValueTask<bool>(false);
    }

    public ValueTask<bool> MoveTo(int index)
    {
        if (index != 0)
        {
            return new ValueTask<bool>(false);
        }

        return new ValueTask<bool>(true);
    }

    public ValueTask<bool> MoveToRandom()
    {
        // Cant move to random
        return new ValueTask<bool>(true);
    }

    private async Task<Option<WaveeContextStream>> AwaitRes(Task<WaveeStream> stream)
    {
        var res = await stream;
        CurrentStream =
            Option<WaveeContextStream>.Some(new WaveeContextStream(res, new ComposedKey(res.Metadata.Id!)));
        return CurrentStream;
    }
}