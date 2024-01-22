using Eum.Spotify.context;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.Spfy.Playback;

internal sealed class LazySpotifyContext : SpotifyRealContext
{
    private readonly Option<WaveeContextStream> _firstStream;

    private Context _context;
    private Context _originalContext;
    private ContextPage? _lastPage;
    private int _seenPages;
    private int _seenTracks;
    private readonly Func<ContextTrack, bool> _predicate;

    public LazySpotifyContext(Guid connectionId,
        Context ctx,
        Func<ContextTrack, bool> predicate,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory,
        Option<WaveeContextStream> firstStream) : base(connectionId,
        SpotifyId.FromUri(ctx.Uri), Option<ValueTask<int>>.None,
        streamFactory)
    {
        _firstStream = firstStream;
        _context = ctx;
        _predicate = predicate;
        _originalContext = ctx.Clone();
        base._startIndex = FindIndex(predicate, firstStream);

        ContextMetadata = new HashMap<string, string>();
        foreach (var metadata in _context.Metadata)
        {
            ContextMetadata = ContextMetadata.Add(metadata.Key, metadata.Value);
        }
    }

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public override HashMap<string, string> ContextMetadata { get; }
    private bool _fetched;

    protected override ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        return NextPageInner(true);
    }

    protected override ValueTask<Option<SpotifyContextPage>> PeekNextPage()
    {
        return NextPageInner(false);
    }


    private async ValueTask<Option<SpotifyContextPage>> NextPageInner(bool mutate)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_lastPage is not null)
            {
                // if (_returnLastPage)
                // {
                //     _returnLastPage = false;
                //     var f = MapToSpotifyContextPage(_lastPage!, _seenPages - 1);
                //     _lastPage = null;
                //     return f;
                // }
                var nextPageUrl = _lastPage.NextPageUrl;
                if (!string.IsNullOrEmpty(nextPageUrl))
                {
                    var nextPage = await FetchPage(nextPageUrl);
                    if (mutate)
                    {
                        _lastPage = nextPage;
                    }

                    var x = MapToSpotifyContextPage(nextPage, Option<int>.None, mutate);
                    // if (addToList)
                    // {
                    //     base._pages.AddLast(x);
                    // }

                    return x;
                }

                return Option<SpotifyContextPage>.None;
            }

            if (!_fetched && _context.Pages.Count == 0)
            {
                var context = await FetchPages(_originalContext.Uri);
                if (mutate)
                {
                    _context = context;
                    _fetched = true;
                }
            }

            //var firstPage = _context.Pages.SkipWhile(x => x == _lastPage).FirstOrDefault();
            ContextPage? firstPage = null;
            bool found = false;
            foreach (var page in _context.Pages)
            {
                if (found)
                {
                    firstPage = page;
                    break;
                }

                if (page == _lastPage)
                {
                    found = true;
                }
            }

            if (firstPage is null)
            {
                firstPage = _context.Pages.FirstOrDefault();
            }

            if (firstPage is null)
            {
                return Option<SpotifyContextPage>.None;
            }

            if (mutate)
            {
                _lastPage = firstPage;
            }

            var y = MapToSpotifyContextPage(firstPage, Option<int>.None, mutate);
            // if (addToList)
            // {
            //     base._pages.AddLast(y);
            // }

            return y;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Option<SpotifyContextPage>.None;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private SpotifyContextPage MapToSpotifyContextPage(ContextPage nextPage, Option<int> pageIndex, bool mutate)
    {
        var idx = _seenPages;
        if (mutate)
        {
            _seenPages++;
        }

        var tracks = new LinkedList<SpotifyContextTrack>();
        for (var index = 0; index < nextPage.Tracks.Count; index++)
        {
            var track = nextPage.Tracks[index];
            var trackIdx = _seenTracks + index;

            SpotifyId id = default;
            if (!string.IsNullOrEmpty(track.Uri))
            {
                id = SpotifyId.FromUri(track.Uri);
            }
            else if (track.HasGid)
            {
                //TODO: Episodes
                id = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track);
            }
            else
            {
                continue;
            }

            Option<string> uid = Option<string>.None;
            if (!string.IsNullOrEmpty(track.Uid))
            {
                uid = track.Uid;
            }
            else if (_itemId.Type is AudioItemType.Album)
            {
                //albumIdTrackId
                uid = Option<string>.Some($"{id.ToBase62().ToString()}{_itemId.ToBase62()}");
            }

            var ctxtrack = new SpotifyContextTrack(id, uid, trackIdx);
            tracks.AddLast(ctxtrack);
        }


        if (mutate)
        {
            _seenTracks += tracks.Count;
        }

        var finalPageIndex = pageIndex.IsNone ? idx : pageIndex;
        var finalPageIndexValue = finalPageIndex.ValueUnsafe();
        return new SpotifyContextPage(tracks, (uint)finalPageIndexValue);
    }

    private async Task<ContextPage> FetchPage(string pageUrl)
    {
        if (EntityManager.TryGetClient(_connectionId, out var client))
        {
            var context = await client.Playback.ResolveContextRaw(pageUrl, CancellationToken.None);
            return context;
        }

        throw new NotSupportedException();
    }

    private async Task<Context> FetchPages(string contextUri)
    {
        if (EntityManager.TryGetClient(_connectionId, out var client))
        {
            var context = await client.Playback.ResolveContext(contextUri);
            return context;
        }

        throw new NotSupportedException();
    }

    private async ValueTask<int> FindIndex(Func<ContextTrack, bool> predicate, Option<WaveeContextStream> firstStream)
    {
        int foundPage = 0;
        int foundIndex = 0;
        int seendTracks = 0;

        var previousContext = _context.Clone();
        var previousPage = _lastPage;
        var previousSeenPages = _seenPages;
        var previousSeenTracks = _seenTracks;


        while (true)
        {
            if (_lastPage is null)
            {
                var fetched = await NextPageInner(true);
                if (fetched.IsNone)
                {
                    break;
                }
            }

            for (var i = 0; i < _lastPage!.Tracks.Count; i++)
            {
                var track = _lastPage.Tracks[i];
                if (predicate(track))
                {
                    foundIndex = seendTracks + i;
                    break;
                }
            }


            seendTracks += _lastPage.Tracks.Count;

            var x = await NextPageInner(true);
            if (x.IsNone)
            {
                break;
            }

            foundPage++;
        }


        _lastPage = previousPage;
        _seenPages = previousSeenPages;
        _seenTracks = previousSeenTracks;
        return foundIndex;
    }
}