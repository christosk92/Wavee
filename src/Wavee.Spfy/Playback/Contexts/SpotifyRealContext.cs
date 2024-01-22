using Eum.Spotify.context;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Contexting;
using static LanguageExt.Prelude;

namespace Wavee.Spfy.Playback.Contexts;

internal abstract class SpotifyRealContext : ISpotifyContext
{
    protected readonly Guid _connectionId;
    protected readonly SpotifyId _itemId;
    protected Option<ValueTask<int>> _startIndex;
    private readonly Func<SpotifyId, CancellationToken, Task<WaveeStream>> _streamFactory;
    protected readonly LinkedList<SpotifyContextPage> _pages = new();
    private Option<LinkedListNode<SpotifyContextPage>> _currentPage = Option<LinkedListNode<SpotifyContextPage>>.None;

    private Option<LinkedListNode<SpotifyContextTrack>>
        _currentTrack = Option<LinkedListNode<SpotifyContextTrack>>.None;

    protected SpotifyRealContext(Guid connectionId,
        SpotifyId itemId,
        Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory)
    {
        _connectionId = connectionId;
        _itemId = itemId;
        _startIndex = startIndex;
        _streamFactory = streamFactory;

        ContextUri = itemId.ToString();
        ContextUrl = $"context://{ContextUri}";
    }

    public string ContextUri { get; }
    public string ContextUrl { get; }
    public abstract HashMap<string, string> ContextMetadata { get; }
    public Option<SpotifyContextPage> CurrentPage => _currentPage.Map(x => x.Value);

    protected abstract ValueTask<Option<SpotifyContextPage>> NextPage();
    protected abstract ValueTask<Option<SpotifyContextPage>> PeekNextPage();

    public async ValueTask<Option<WaveeContextStream>> GetNextStream()
    {
        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        bool skipped = false;
        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            _startIndex = Prelude.None;
            if (!(await TrySkip(startIndexVal)))
            {
                return Prelude.None;
            }

            skipped = true;
            currentPage = _currentPage;
            currentTrack = _currentTrack;
        }

        if (currentPage == Prelude.None || currentTrack == Prelude.None)
        {
            // No current page or current track, fetch the first page
            var firstPage = await NextPage();
            if (firstPage.IsSome)
            {
                currentPage = _pages.AddLast(firstPage.ValueUnsafe());
                _currentPage = currentPage;
                currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                _currentTrack = currentTrack;
            }
            else
            {
                // No more pages, no more tracks
                CurrentStream = Option<WaveeContextStream>.None;
                return Option<WaveeContextStream>.None;
            }
        }
        else
        {
            // Move to the next track in the current page
            //currentTrack = currentTrack.ValueUnsafe().Next;
            currentTrack = currentTrack.Bind(x =>
            {
                if (skipped)
                    return x;
                var next = x.Next;
                if (next == null)
                {
                    return Prelude.None;
                }

                return Prelude.Some(next);
            });
            _currentTrack = currentTrack;

            if (currentTrack == Option<LinkedListNode<SpotifyContextTrack>>.None)
            {
                // Reached the end of the current page, fetch the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks
                    CurrentStream = Option<WaveeContextStream>.None;
                    return Option<WaveeContextStream>.None;
                }
            }
        }

        if (_currentTrack == Option<LinkedListNode<SpotifyContextTrack>>.None &&
            _currentPage != Option<LinkedListNode<SpotifyContextPage>>.None)
        {
            _currentTrack = _currentPage.ValueUnsafe().Value.Tracks.First;
        }

        var waveeStream = await _streamFactory(_currentTrack.ValueUnsafe().Value.Gid, CancellationToken.None);
        var keys = new List<object>(5);
        if (_currentTrack.ValueUnsafe().Value.Uid.IsSome)
        {
            keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Uid,
                _currentTrack.ValueUnsafe().Value.Uid.ValueUnsafe()));
        }

        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Provider, "context"));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Id, waveeStream.Metadata.Id));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Index,
            _currentTrack.ValueUnsafe().Value.Index.ToString()));
        //page index
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.PageIndex,
            _currentPage.ValueUnsafe().Value.Index.ToString()));
        var waveeContextStream = new WaveeContextStream(waveeStream, ComposedKey.FromKeys(keys));
        CurrentStream = Option<WaveeContextStream>.Some(waveeContextStream);
        return Option<WaveeContextStream>.Some(waveeContextStream);
    }

    public ValueTask<Option<WaveeContextStream>> GetPreviousStream()
    {
        throw new NotImplementedException();
    }

    public Option<WaveeContextStream> CurrentStream { get; private set; }

    public async ValueTask<bool> TrySkip(int count)
    {
        if (count is 0)
            return true;
        if (count < 0)
        {
            return false; // Invalid count, cannot skip non-positive number of tracks
        }

        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            count += startIndexVal;
        }

        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        // Iterate through pages and tracks to skip the specified count
        while (count > 0)
        {
            if (currentPage == Prelude.None || currentTrack == Prelude.None)
            {
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = currentPage.ValueUnsafe().Value.Tracks.Count -
                                        (currentTrack.ValueUnsafe().Value.Index + 1);
            if (count <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < count; i++)
                {
                    if (currentTrack == Option<LinkedListNode<SpotifyContextTrack>>.None)
                    {
                        currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                        _currentTrack = currentTrack;
                        continue;
                    }
                    currentTrack = currentTrack.ValueUnsafe().Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await NextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                            _currentPage = currentPage;
                            currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                            _currentTrack = currentTrack;
                        }
                        else
                        {
                            // No more pages, no more tracks to skip
                            return false;
                        }
                    }
                }

                count = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                count -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }
        }

        // Set the updated current track
        _currentTrack = currentTrack;

        return true;
    }

    public async ValueTask<bool> TryPeek(int count)
    {
        if (count <= 0)
        {
            return false; // Invalid count, cannot skip non-positive number of tracks
        }

        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        var pagesCopy = new LinkedList<SpotifyContextPage>(_pages);

        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            count += startIndexVal;
        }

        // Iterate through pages and tracks to skip the specified count
        while (count > 0)
        {
            if (currentPage == Prelude.None && currentTrack == Prelude.None)
            {
                var nextPage = await PeekNextPage();
                if (nextPage.IsSome)
                {
                    currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                    //_currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    //_currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = currentPage.ValueUnsafe().Value.Tracks.Count -
                                        (currentTrack.Match(
                                            Some: x => x.Value.Index + 1,
                                            None: () => 0
                                        ));
            if (count <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < count; i++)
                {
                    if (currentTrack == Option<LinkedListNode<SpotifyContextTrack>>.None)
                    {
                        currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                        continue;
                    }

                    currentTrack = currentTrack.ValueUnsafe().Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await PeekNextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                            //_currentPage = currentPage;
                            currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                            //   _currentTrack = currentTrack;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                count = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                count -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await PeekNextPage();
                if (nextPage.IsSome)
                {
                    currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                    // _currentPage = currentPage;

                    //Setting currentTrack to null will cause the next iteration to fetch the first track in the page
                    currentTrack = null;
                    // _currentTrack = currentTrack;
                }
                else
                {
                    return false;
                }
            }
        }

        // Set the updated current track
        //   _currentTrack = currentTrack;

        return true;
    }

    public async ValueTask<bool> MoveTo(int index)
    {
        if (index < 0)
        {
            return false;
        }

        //we will always move one too much because FetchNextStream will move to the next track
        if (index is 0)
        {
            _currentPage = _pages.First;
            _currentTrack = null;
            return true;
        }
        else
        {
            index -= 1;
        }

        //Keep in mind, index is an absolute index, not relative to the current page
        //meanaing we might have 20 pages, and index 100, which means we need to fetch 5 pages
        LinkedListNode<SpotifyContextPage>? currentPage = _pages.First;
        LinkedListNode<SpotifyContextTrack>? currentTrack = currentPage?.Value.Tracks?.First;
        while (index > 0)
        {
            if (currentPage == null && currentTrack == null)
            {
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = 0;
            if (currentTrack is null)
            {
                remainingTracksInPage = currentPage.Value.Tracks.Count;
            }
            else
            {
                remainingTracksInPage = currentPage.Value.Tracks.Count -
                                        (currentTrack.Value.Index + 1);
            }

            if (index <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < index; i++)
                {
                    currentTrack = currentTrack?.Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await NextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                            _currentPage = currentPage;
                            currentTrack = currentPage.Value.Tracks.First;
                            _currentTrack = currentTrack;
                        }
                        else
                        {
                            // No more pages, no more tracks to skip
                            return false;
                        }
                    }
                }

                index = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                index -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = null;
                    _currentTrack = null;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }
        }

        _currentTrack = currentTrack;
        return true;
    }
}

internal sealed class SpotifyPlaylistOrAlbumContext : SpotifyRealContext
{
    //private Option<Option<ContextPage>> _currentPage;
    private Option<Queue<ContextPage>> _pages = new();
    private Option<ContextPage> _lastPage = Option<ContextPage>.None;
    private int _tracksBefore = 0;
    private int _pagesBefore = 0;
    private HashMap<string, string> _contextMetadata = LanguageExt.HashMap<string, string>.Empty;

    public SpotifyPlaylistOrAlbumContext(Guid connectionId, SpotifyId itemId, Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex,
        streamFactory)
    {
    }


    public override HashMap<string, string> ContextMetadata => _contextMetadata;

    protected override async ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        if (_pages.IsNone)
        {
            // initialize !
            // 1) Context-Resolve
            // 2) Add pages
            if (!EntityManager.TryGetClient(_connectionId, out var client))
            {
                throw new InvalidOperationException("Client not found");
            }

            var contextResolveResponse = await client.Playback.ResolveContext(_itemId.ToString());
            var pages = new Queue<ContextPage>();
            foreach (var pageInContext in contextResolveResponse.Pages)
            {
                pages.Enqueue(pageInContext);
            }

            foreach (var metadatakey in contextResolveResponse.Metadata)
            {
                _contextMetadata = _contextMetadata.Add(metadatakey.Key, metadatakey.Value);
            }

            _pages = Some(pages);
        }

        var pagesValue = _pages.ValueUnsafe();
        if (!pagesValue.TryDequeue(out var page))
        {
            if (_lastPage.IsNone)
                return None;

            var lastPageValue = _lastPage.ValueUnsafe();
            // TODO: Try fetch next page
            return Option<SpotifyContextPage>.None;
        }

        var x = MapToSpotifyContextPage(page, true);

        _lastPage = Some(page);
        return Some(x);
    }

    protected override async ValueTask<Option<SpotifyContextPage>> PeekNextPage()
    {
        if (_pages.IsNone)
        {
            // initialize !
            // 1) Context-Resolve
            // 2) Add pages
            if (!EntityManager.TryGetClient(_connectionId, out var client))
            {
                throw new InvalidOperationException("Client not found");
            }

            var contextResolveResponse = await client.Playback.ResolveContext(_itemId.ToString());
            var pages = new Queue<ContextPage>();
            foreach (var pageInContext in contextResolveResponse.Pages)
            {
                pages.Enqueue(pageInContext);
            }

            foreach (var metadatakey in contextResolveResponse.Metadata)
            {
                _contextMetadata = _contextMetadata.Add(metadatakey.Key, metadatakey.Value);
            }

            _pages = Some(pages);
        }

        var pagesValue = _pages.ValueUnsafe();
        if (!pagesValue.TryPeek(out var page))
        {
            if (_lastPage.IsNone)
                return None;

            var lastPageValue = _lastPage.ValueUnsafe();
            // TODO: Try fetch next page
            return Option<SpotifyContextPage>.None;
        }

        var x = MapToSpotifyContextPage(page, false);

        _lastPage = Some(page);
        return Some(x);
    }

    private SpotifyContextPage MapToSpotifyContextPage(ContextPage result, bool mutate)
    {
        var tracks = new LinkedList<SpotifyContextTrack>();
        for (int i = 0; i < result.Tracks.Count; i++)
        {
            var track = result.Tracks[i];
            SpotifyId id = default;
            if (!string.IsNullOrEmpty(track.Uri))
            {
                id = SpotifyId.FromUri(track.Uri);
            }
            else if (track.Gid.Span.Length > 0)
            {
                id = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track);
            }
            else
            {
                continue;
            }

            tracks.AddLast(new SpotifyContextTrack(id, track.Uid, i));
        }

        if (mutate)
        {
            _tracksBefore += result.Tracks.Count;
            _pagesBefore++;
        }

        {
        }

        return new SpotifyContextPage(tracks, (uint)(_pagesBefore - 1));
    }
}

internal sealed class SpotifyArtistContext : SpotifyRealContext
{
    public SpotifyArtistContext(Guid connectionId, SpotifyId itemId, Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex,
        streamFactory)
    {
    }

    public override HashMap<string, string> ContextMetadata { get; }

    protected override ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        throw new NotImplementedException();
    }

    protected override ValueTask<Option<SpotifyContextPage>> PeekNextPage()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    private async Task<Option<WaveeContextStream>> AwaitRes(Task<WaveeStream> stream)
    {
        var res = await stream;
        CurrentStream =
            Option<WaveeContextStream>.Some(new WaveeContextStream(res, new ComposedKey(res.Metadata.Id!)));
        return CurrentStream;
    }
}