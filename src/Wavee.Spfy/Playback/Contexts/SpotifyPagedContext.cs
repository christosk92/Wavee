using Eum.Spotify.context;
using LanguageExt;

namespace Wavee.Spfy.Playback.Contexts;

internal abstract class SpotifyPagedContext : SpotifyRealContext
{
    private readonly LinkedList<ContextPage> _fetchedPagesCache = new LinkedList<ContextPage>();
    private LinkedListNode<ContextPage>? _currentPageNode;
    private bool _intialPageFetched;
    private HashMap<string, string> _contextMetadata;

    protected SpotifyPagedContext(Guid connectionId,
        Context context,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> createSpotifyStream)
        : base(
            connectionId: connectionId,
            contextUri: context.Uri,
            contextUrl: context.Url,
            streamFactory: createSpotifyStream)
    {
        _contextMetadata = context.Metadata.ToHashMap();
        foreach (var page in context.Pages)
        {
            _fetchedPagesCache.AddLast(page);
        }
    }

    public override HashMap<string, string> ContextMetadata => _contextMetadata;
    
    protected override async ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        var lastActivePage = _currentPageNode?.Value;
        bool gonext = true;
        //If the current node is null and we have already fetched the first page, it means we have reached the end of the context
        if (_currentPageNode is null && _intialPageFetched)
        {
            _currentPageNode = _fetchedPagesCache.First;
            if (_currentPageNode is null)
            {
                //We have reached the end of the context
                return Option<SpotifyContextPage>.None;
            }
            else
            {
                gonext = false;
            }
        }

        //If the first node is null, it means we haven't fetched the first page yet
        if (_currentPageNode is null)
        {
            //Or maybe we have, but it was empty
            _currentPageNode = _fetchedPagesCache.First;
            if (_currentPageNode is null)
            {
                //Right, we haven't fetched the first page yet
                var ctx = await ResolveContext();
                _contextMetadata = ctx.Metadata.ToHashMap();
                foreach (var page in ctx.Pages)
                {
                    _fetchedPagesCache.AddLast(page);
                }

                // Set to true, and then recurse
                _intialPageFetched = true;
                return await NextPage();
            }
        }
        else if (gonext)
        {
            _currentPageNode = _currentPageNode.Next;
        }

        //Is this check really necessary ?
        if (_currentPageNode is null && lastActivePage is not null)
        {
            if (lastActivePage.HasNextPageUrl && !string.IsNullOrEmpty(lastActivePage.NextPageUrl))
            {
                var tracks = await ResolvePage(lastActivePage.NextPageUrl);
                _currentPageNode = _fetchedPagesCache.AddLast(tracks);
            }
        }

        if (_currentPageNode is null)
        {
            //We have reached the end of the context
            return Option<SpotifyContextPage>.None;
        }

        var currentPage = _currentPageNode.Value;
        if (currentPage.Tracks.Count is 0)
        {
            //empty track list, do we even have the page?
            if (currentPage.HasPageUrl && !string.IsNullOrEmpty(currentPage.PageUrl))
            {
                var tracks = await ResolvePage(currentPage.PageUrl);
                currentPage.Tracks.AddRange(tracks.Tracks);
                currentPage.PageUrl = string.Empty;
                currentPage.NextPageUrl = tracks.NextPageUrl;
            }
        }

        return ConstructPage(currentPage);
    }

    private SpotifyContextPage ConstructPage(ContextPage currentPage)
    {
        var idx = 0;
        foreach (var page in _fetchedPagesCache)
        {
            if (page == currentPage)
            {
                break;
            }

            idx += page.Tracks.Count;
        }


        var tracks = new LinkedList<SpotifyContextTrack>();
        for (var index = 0; index < currentPage.Tracks.Count; index++)
        {
            var track = currentPage.Tracks[index];
            SpotifyId id = default;
            if (track.HasUri && !string.IsNullOrEmpty(track.Uri))
            {
                id = SpotifyId.FromUri(track.Uri);
            }
            else if (track.HasGid && !track.Gid.IsEmpty)
            {
                //TODO :Episodes
                id = SpotifyId.FromRaw(track.Gid.ToByteArray(), AudioItemType.Track);
            }
            else
            {
                continue;
            }

            var metadata = track.Metadata.ToHashMap();
            Option<string> uid = Option<string>.None;
            if (track.HasUid && !string.IsNullOrEmpty(track.Uid))
            {
                uid = track.Uid;
            }

            tracks.AddLast(new SpotifyContextTrack(id, uid, index, metadata));
        }

        return new SpotifyContextPage(tracks, (uint)idx);
    }

    private async Task<ContextPage> ResolvePage(string currentPagePageUrl)
    {
        if (EntityManager.TryGetClient(_connectionId, out var client))
        {
            var context = await client.Playback.ResolveContextRaw(currentPagePageUrl, CancellationToken.None);
            return context;
        }

        throw new InvalidOperationException("Client not found");
    }

    private async Task<Context> ResolveContext()
    {
        if (EntityManager.TryGetClient(_connectionId, out var client))
        {
            var context = await client.Playback.ResolveContext(this.ContextUri);
            return context;
        }

        throw new NotSupportedException();
    }
}