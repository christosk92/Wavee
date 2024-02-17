using Eum.Spotify.context;
using Wavee.Core;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Playback.Contexting;

internal sealed class SpotifyPlayContext : IWaveePlayContext
{
    private readonly string _contextUri;
    private readonly string _contextUrl;
    private LinkedList<ContextPage>? _pages;
    private readonly IContextClient _contextClient;

    public SpotifyPlayContext(string contextUri, string contextUrl, IReadOnlyList<ContextPage>? pages,
        IContextClient contextClient)
    {
        _contextUri = contextUri;
        _contextUrl = contextUrl;
        _contextClient = contextClient;
        if (pages is not null)
        {
            _pages = new LinkedList<ContextPage>(pages);
        }
    }

    public async ValueTask<IWaveeMediaSource?> GetAt(int index, CancellationToken cancellationToken = default)
    {
        if (_pages is null || _pages.Count == 0)
        {
            await InitializePages(cancellationToken);
        }

        int cumulativeIndex = 0;
        var currentNode = _pages?.First;
        while (currentNode != null)
        {
            var page = await EnsurePageLoadedAsync(currentNode, cancellationToken);
            if (page == null || page.Tracks == null)
            {
                currentNode = currentNode.Next;
                continue;
            }

            int nextPageCumulativeIndex = cumulativeIndex + page.Tracks.Count;
            if (index < nextPageCumulativeIndex)
            {
                int trackIndexInPage = index - cumulativeIndex;
                if (trackIndexInPage < page.Tracks.Count)
                {
                    var track = page.Tracks[trackIndexInPage];
                    return await CreateMediaSource(track, cancellationToken);
                }

                break;
            }

            cumulativeIndex = nextPageCumulativeIndex;
            currentNode = currentNode.Next;
        }

        return null;
    }

    public async Task<(int AbsoluteIndex, int IndexInPage, int PageIndex)> FindAsync(
        int? pageIndex = null,
        int? trackIndex = null,
        string? trackUid = null,
        SpotifyId? trackId = null,
        CancellationToken cancellationToken = default)
    {
        if (_pages is null || _pages.Count == 0)
        {
            await InitializePages(cancellationToken);
        }

        int absoluteIndex = 0;
        var currentNode = _pages?.First;
        int pageIndexCounter = 0;
        while (currentNode != null)
        {
            var page = await EnsurePageLoadedAsync(currentNode, cancellationToken);
            if (page == null || page.Tracks == null)
            {
                currentNode = currentNode.Next;
                pageIndexCounter++;
                continue;
            }

            // Priority 1: Direct index access if within bounds
            if (pageIndex.HasValue && pageIndex.Value == pageIndexCounter)
            {
                if (trackIndex.HasValue && trackIndex.Value < page.Tracks.Count)
                {
                    return (absoluteIndex + trackIndex.Value, trackIndex.Value, pageIndexCounter);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Track index is out of bounds.");
                }
            }

            // Priority 2 and 3: Search by trackUid or trackId
            for (int j = 0; j < page.Tracks.Count; j++)
            {
                var track = page.Tracks[j];
                if (trackUid != null && track.Uid == trackUid ||
                    trackId != null && (track.Uri == trackId.Value.ToString() || track.Gid.Span.SequenceEqual(trackId.Value.Id.ToByteArray(true, true))))
                {
                    return (absoluteIndex + j, j, pageIndexCounter);
                }
            }

            absoluteIndex += page.Tracks.Count;
            currentNode = currentNode.Next;
            pageIndexCounter++;
        }

        // Priority 4: Default case, return the first track if available
        if (_pages.First.Value.Tracks == null || !_pages.First.Value.Tracks.Any())
        {
            throw new InvalidOperationException("No tracks available in the context.");
        }

        return (0, 0, 0);
    }

    private async Task<ContextPage?> FetchPageByNextPageUrl(string pageNextPageUrl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<ContextPage?> FetchPageByPageUrl(string pagePageUrl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<IWaveeMediaSource?> CreateMediaSource(ContextTrack track, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task InitializePages(CancellationToken cancellationToken)
    {
        var context = await _contextClient.ResolveContext(_contextUri, cancellationToken);
        _pages = new LinkedList<ContextPage>(context.Pages);
    }

    private async Task<ContextPage?> EnsurePageLoadedAsync(LinkedListNode<ContextPage> currentNode,
        CancellationToken cancellationToken)
    {
        var page = currentNode.Value;
        if (page.Tracks != null) return page; // Page is already loaded

        ContextPage? updatedPage = null;
        if (!string.IsNullOrEmpty(page.PageUrl))
        {
            updatedPage = await FetchPageByPageUrl(page.PageUrl, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(page.NextPageUrl))
        {
            updatedPage = await FetchPageByNextPageUrl(page.NextPageUrl, cancellationToken);
            if (updatedPage != null)
            {
                _pages.AddAfter(currentNode, updatedPage);
            }
        }

        if (updatedPage != null)
        {
            currentNode.Value = updatedPage;
            return updatedPage;
        }

        return null;
    }
}