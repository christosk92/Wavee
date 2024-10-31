using NeoSmart.AsyncLock;
using Wavee.Enums;
using Wavee.Models.Common;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;

namespace Wavee.Playback;

internal sealed class TrackQueue
{
    private readonly AsyncLock _lock = new();

    private int _currentTrackIndex = -1;
    private int _currentPageIndex = -1;
    private List<WaveePlayerPlaybackContextPage>? _pages = null;
    private WaveePlayerPlaybackContext? _context;
    private readonly Queue<WaveePlayerMediaItem> _queue = new();
    public WaveePlayerPlaybackContext? Context => _context;
    public RepeatMode RepeatMode { get; private set; }
    public bool Shuffle { get; private set; }

    public void Reset()
    {
        using (_lock.Lock())
        {
            _currentTrackIndex = -1;
            _currentPageIndex = -1;
            _pages = null;

            _context?.Dispose();
            _context = null;
        }
    }

    public void Enqueue(WaveePlayerMediaItem mediaItem)
    {
        using (_lock.Lock())
        {
            _queue.Enqueue(mediaItem);
        }
    }


    public async Task Initialize(WaveePlayerPlaybackContext context, CancellationToken cancellationToken)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            _context?.Dispose();
            _context = null;

            _context = context;
            _currentTrackIndex = -1;
            _currentPageIndex = -1;
            _pages = null;

            Task.Run(async () =>
            {
                using (await _lock.LockAsync(cancellationToken))
                {
                    var pages = await _context.InitializePages();
                    _pages = [..pages];
                }
            });
        }
    }

    public List<WaveePlayerMediaItem> GetPreviousItems()
    {
        using (_lock.Lock())
        {
            if (_pages == null)
                return [];

            var items = new List<WaveePlayerMediaItem>();
            for (var index = _currentPageIndex; index >= 0; index--)
            {
                // check if the page is available
                var pageInRange = index >= 0 && index < _pages.Count;
                if (!pageInRange)
                {
                    break;
                }

                var page = _pages[index];
                if (!page.PeekTracks(out var tracks))
                {
                    // replace with pseudo page:
                    var metadata = new Dictionary<string, string>();
                    metadata["hidden"] = "true";
                    var pageUid = $"page{index}_0";
                    var uri = SpotifyId.FromUri($"spotify:meta:page:{index}");
                    var pageItem = new WaveePlayerMediaItem(uri, pageUid, metadata);
                    items.Add(pageItem);
                }
                else
                {
                    if (_currentPageIndex == index)
                    {
                        items.AddRange(tracks!.Take(_currentTrackIndex));
                    }
                    else
                    {
                        items.AddRange(tracks!);
                    }
                }
            }

            return items;
        }
    }

    public List<WaveePlayerMediaItem> GetFutureItems()
    {
        using (_lock.Lock())
        {
            if (_pages == null)
                return [];

            var items = new List<WaveePlayerMediaItem>();
            foreach (var queueItem in _queue)
            {
                items.Add(queueItem);
            }

            for (var index = _currentPageIndex; index < _pages.Count; index++)
            {
                // check if the page is available
                var pageInRange = index >= 0 && index < _pages.Count;
                if (!pageInRange)
                {
                    break;
                }

                var page = _pages[index];
                if (!page.PeekTracks(out var tracks))
                {
                    // replace with pseudo page:
                    var metadata = new Dictionary<string, string>();
                    metadata["hidden"] = "true";
                    var pageUid = $"page{index}_0";
                    var uri = SpotifyId.FromUri($"spotify:meta:page:{index}");
                    var pageItem = new WaveePlayerMediaItem(uri, pageUid, metadata);
                    items.Add(pageItem);
                }
                else
                {
                    if (_currentPageIndex == index)
                    {
                        items.AddRange(tracks!.Skip(_currentTrackIndex + 1));
                    }
                    else
                    {
                        items.AddRange(tracks!);
                    }
                }
            }

            return items;
        }
    }

    public WaveePlayerMediaItem Previous()
    {
        using (_lock.Lock())
        {
            if (_pages == null || _pages.Count == 0)
                throw new InvalidOperationException("No pages are loaded.");

            if (_currentPageIndex == -1 && _currentTrackIndex == -1)
            {
                // Start from the last track of the last page
                _currentPageIndex = _pages.Count - 1;

                var page = _pages[_currentPageIndex];
                if (!page.PeekTracks(out var tracks))
                {
                    throw new InvalidOperationException("Tracks not loaded in the page.");
                }

                _currentTrackIndex = tracks.Count - 1;
            }
            else
            {
                _currentTrackIndex--;

                while (_currentTrackIndex < 0)
                {
                    _currentPageIndex--;

                    if (_currentPageIndex < 0)
                    {
                        // Reached the beginning of the playlist
                        throw new InvalidOperationException("No previous track available.");
                    }

                    var page = _pages[_currentPageIndex];
                    if (!page.PeekTracks(out var tracks))
                    {
                        throw new InvalidOperationException("Tracks not loaded in the page.");
                    }

                    _currentTrackIndex = tracks.Count - 1;
                }
            }

            var currentPage = _pages[_currentPageIndex];
            if (!currentPage.PeekTracks(out var currentTracks))
            {
                throw new InvalidOperationException("Tracks not loaded in the current page.");
            }

            var currentTrack = currentTracks[_currentTrackIndex];
            return currentTrack;
        }
    }

    public async Task SetStartingItem(WaveePlayerMediaItem mediaItem, CancellationToken ct)
    {
        using (await _lock.LockAsync())
        {
            if (_pages is null)
            {
                _currentPageIndex = 0;
                _currentTrackIndex = -1;
                var page = await _context!.GetPage(_currentPageIndex, ct);
                if (page is null)
                    throw new InvalidOperationException("No pages are loaded.");
                _pages = [page];
            }

            // Start from scratch
            _currentPageIndex = -1;
            _currentTrackIndex = -1;
            while (true)
            {
                var item = await NextItem(false, ct);
                if (item is null)
                    throw new InvalidOperationException("No items in the playlist.");
                // try to match with UID first
                if (!string.IsNullOrEmpty(mediaItem.Uid) && !string.IsNullOrEmpty(item.Uid) &&
                    mediaItem.Uid == item.Uid)
                {
                    return;
                }

                if (mediaItem.ContainsId(item.Id.Value))
                {
                    return;
                }
                // if not found, enqueue the item
            }
        }
    }

    public async Task<WaveePlayerMediaItem> SetStartingIndex(int pageIndex, int trackIndex,
        CancellationToken cancellationToken)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            // start from scratch again
            _currentPageIndex = -1;
            _currentTrackIndex = -1;

            // If _pages is null, initialize it with the first page
            if (_pages == null)
            {
                _pages = new List<WaveePlayerPlaybackContextPage>();
                var firstPage = await _context!.GetPage(0, cancellationToken);
                if (firstPage == null)
                    throw new InvalidOperationException("No pages are available in the context.");
                _pages.Add(firstPage);
            }

            // Ensure that pages up to the desired pageIndex are loaded
            for (int i = _pages.Count; i <= pageIndex; i++)
            {
                var page = await _context!.GetPage(i, cancellationToken);
                if (page == null)
                    throw new InvalidOperationException($"Page {i} could not be loaded.");
                _pages.Add(page);
            }

            // Update current indices
            _currentPageIndex = pageIndex;
            _currentTrackIndex = trackIndex;

            var currentPage = _pages[_currentPageIndex];

            // Initialize the page if not already initialized
            var currentItem = await currentPage.GetItemAt(_currentTrackIndex, cancellationToken);
            if (currentItem == null)
                throw new InvalidOperationException($"Track at index {trackIndex} on page {pageIndex} does not exist.");

            return currentItem;
        }
    }


    public void ToggleShuffle(bool value)
    {
        //TODO:
    }

    public async Task<WaveePlayerMediaItem?> NextItem(bool acquireLock, CancellationToken cancellationToken)
    {
        async Task<WaveePlayerMediaItem> Inner()
        {
            // First, check if there are any items in the queue
            if (_queue.TryDequeue(out var queuedItem))
            {
                // Return the next item from the queue
                return queuedItem;
            }

            if (_pages == null || _pages.Count == 0)
                throw new InvalidOperationException("No pages are loaded.");

            // If playback hasn't started yet, start from the first track of the first page
            if (_currentPageIndex == -1 && _currentTrackIndex == -1)
            {
                _currentPageIndex = 0;
                _currentTrackIndex = 0;
            }
            else
            {
                _currentTrackIndex++;
            }

            while (true)
            {
                // Check if we've reached the end of the pages
                if (_currentPageIndex >= _pages.Count)
                {
                    var nextPage = await _context!.GetPage(_currentPageIndex, cancellationToken);
                    if (nextPage == null)
                    {
                        // Reached the end of the playlist
                        return null;
                    }

                    _pages.Add(nextPage);
                    continue;
                }

                var currentPage = _pages[_currentPageIndex];

                // Try to get the item at the current index
                var currentItem = await currentPage.GetItemAt(_currentTrackIndex, cancellationToken);

                if (currentItem != null)
                {
                    // Found a valid track, return it
                    return currentItem;
                }
                else
                {
                    // Move to the next page
                    _currentPageIndex++;
                    _currentTrackIndex = 0;
                }
            }
        }

        if (acquireLock)
        {
            using (await _lock.LockAsync(cancellationToken))
            {
                return await Inner();
            }
        }

        return await Inner();
    }

    public async Task<WaveePlayerMediaItem?> ResetToStart(CancellationToken cancellationToken)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            _currentPageIndex = -1;
            _currentTrackIndex = -1;
            return await NextItem(false, cancellationToken);
        }
    }

    public void SetRepeatMode(RepeatMode repeatCmdMode)
    {
        //TODO
    }
}