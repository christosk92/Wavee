using System.Security.Cryptography;
using NeoSmart.AsyncLock;
using Wavee.Enums;
using Wavee.Playback.Contexts;
using Wavee.Playback.Player;

namespace ConsoleApp1.Player;

internal sealed class WaveePlayerTrackQueue
{
    private readonly AsyncLock _lock = new();
    private int _currentTrackIndex = -1;
    private int _currentPageIndex = -1;
    private readonly Queue<WaveePlayerMediaItem> _manualQueue = new();
    public WaveePlayerPlaybackContext? Context { get; private set; }
    private Stack<WaveePlayerMediaItem>? _shuffleHistory;

    public void Shuffle(bool shuffle)
    {
        using (_lock.Lock())
        {
            _shuffleHistory = shuffle ? [] : null;
            Shuffling = shuffle;
        }
    }

    public void SetRepeatMode(RepeatMode repeatMode)
    {
        using (_lock.Lock())
        {
            RepeatMode = repeatMode;
        }
    }

    public void Enqueue(WaveePlayerMediaItem item)
    {
        using (_lock.Lock())
        {
            int nqid = (_manualQueue.MaxBy(x => x.QueueId ?? -1)?.QueueId ?? -1) + 1;
            item.QueueId = nqid;
            _manualQueue.Enqueue(item);
        }
    }

    public RepeatMode RepeatMode { get; private set; }
    public bool Shuffling { get; private set; }

    public async Task FromContext(WaveePlayerPlaybackContext context, WaveePlayerMediaItem startFrom)
    {
        using (await _lock.LockAsync())
        {
            if (context == null)
            {
                return;
            }

            if (Context?.Id != context.Id)
            {
                Context?.Dispose();
                Context = null;
                Context = context;
                await context.InitializePages();
            }

            bool? found = null;
            _currentPageIndex = -1;
            _currentTrackIndex = -1;
            while (found is null)
            {
                _currentPageIndex++;
                _currentTrackIndex = -1;
                var page = await context.GetPage(_currentPageIndex, CancellationToken.None);
                if (page is null)
                {
                    found = false;
                    break;
                }

                while (found is null)
                {
                    _currentTrackIndex++;
                    var item = await page.GetItemAt(_currentTrackIndex, CancellationToken.None);
                    if (item is null)
                    {
                        found = false;
                        break;
                    }

                    if (item == startFrom)
                    {
                        startFrom = item;
                        found = true;
                        break;
                    }
                }
            }
        }
    }

    public async Task<WaveePlayerMediaItem?> Previous()
    {
        using (await _lock.LockAsync())
        {
            if (Shuffling)
            {
                // If we have a shuffle history, we can just pop the last item
                if (_shuffleHistory is not null && _shuffleHistory.Count > 0)
                {
                    return _shuffleHistory.Pop();
                }

                // return a random item
                return await GetRandomItem();
            }

            // go back one track
            while (true)
            {
                var page = await Context!.GetPage(_currentPageIndex, CancellationToken.None);
                if (page is null)
                {
                    // return the first track
                    _currentPageIndex = 0;
                    _currentTrackIndex = 1;
                    continue;
                }

                if (_currentTrackIndex is -1)
                {
                    var pageTracks = await page.GetTracks();
                    if (pageTracks.Count == 0)
                    {
                        _currentPageIndex--;
                        continue;
                    }

                    // set the current track to the last track in the page
                    _currentTrackIndex = pageTracks.Count;
                    // note that while the index is 0-based, the count is 1-based,
                    // but the next line will decrement the index to point to the last track
                }

                var prevIndex = _currentTrackIndex - 1;
                if (prevIndex < 0)
                {
                    _currentPageIndex--;
                    continue;
                }

                var item = await page.GetItemAt(prevIndex, CancellationToken.None);
                if (item is null)
                {
                    _currentPageIndex--;
                    _currentTrackIndex = -1;
                    continue;
                }

                _currentTrackIndex = prevIndex;
                return item;
            }
        }
    }

    public Task<WaveePlayerMediaItem?> Next()
    {
        return NextInternal(true);
    }

    public Task<WaveePlayerMediaItem?> PeekNext()
    {
        return NextInternal(false);
    }

    private async Task<WaveePlayerMediaItem?> NextInternal(bool mutate)
    {
        using (await _lock.LockAsync())
        {
            if (mutate)
            {
                if (_manualQueue.TryDequeue(out var q))
                {
                    return q;
                }
            }
            else if (_manualQueue.TryPeek(out var q))
            {
                return q;
            }

            if (RepeatMode is RepeatMode.Track)
            {
                var currentPage = await Context!.GetPage(_currentPageIndex, CancellationToken.None);
                if (currentPage is null)
                {
                    return null;
                }

                var item = await currentPage.GetItemAt(_currentTrackIndex, CancellationToken.None);
                if (item is null)
                {
                    return null;
                }

                return item;
            }

            if (Shuffling)
            {
                if (mutate)
                {
                    var currentPage = await Context!.GetPage(_currentPageIndex, CancellationToken.None);
                    var currentItem = await currentPage!.GetItemAt(_currentTrackIndex, CancellationToken.None);
                    if (currentItem is not null)
                    {
                        _shuffleHistory!.Push(currentItem);
                    }
                }

                var nextRandomItem = await GetRandomItem();
                if (nextRandomItem is null)
                {
                    return null;
                }

                // add to queue
                _manualQueue.Enqueue(nextRandomItem);
                return nextRandomItem;
            }

            int currentPageIndex = _currentPageIndex;
            int currentTrackIndex = _currentTrackIndex;
            WaveePlayerMediaItem? nextItem = null;
            while (true)
            {
                var page = await Context.GetPage(currentPageIndex, CancellationToken.None);
                if (page is null)
                {
                    if (RepeatMode is RepeatMode.Context)
                    {
                        // return start
                        currentPageIndex = 0;
                        currentTrackIndex = -1;
                        continue;
                    }

                    break;
                }

                var elementAt = await page.GetItemAt(currentTrackIndex + 1, CancellationToken.None);
                if (elementAt is null)
                {
                    currentPageIndex++;
                    continue;
                }

                nextItem = elementAt;
                break;
            }

            if (mutate)
            {
                _currentPageIndex = currentPageIndex;
                _currentTrackIndex = currentTrackIndex;
            }

            return nextItem;
        }
    }

    private async Task<WaveePlayerMediaItem?> GetRandomItem()
    {
        var pages = await Context!.InitializePages();
        var randomPage = RandomNumberGenerator.GetInt32(0, pages.Count);
        var page = pages.ElementAt(randomPage);
        var items = await page.GetTracks();
        var randomItem = RandomNumberGenerator.GetInt32(0, items.Count);
        _currentPageIndex = randomPage;
        _currentTrackIndex = randomItem;
        return items.ElementAt(randomItem);
    }

    public void Reset()
    {
        _currentPageIndex = 0;
        _currentTrackIndex = -1;
    }
}