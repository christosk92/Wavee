using System.Diagnostics;
using Eum.Spotify.context;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Enums;
using Wavee.Helpers;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Playback.Contexts;

/// <summary>
/// Represents a stateless context for retrieving pages of a context.
/// </summary>
public abstract class WaveePlayerPlaybackContext : IDisposable
{
    /// <summary>
    /// Retrieves a page of playback context.
    /// </summary>
    /// <param name="pageIndex">The index of the page to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the requested page or null if not found.</returns>
    public abstract Task<WaveePlayerPlaybackContextPage?> GetPage(int pageIndex, CancellationToken cancellationToken);

    public abstract string Id { get; }

    public abstract Task<IReadOnlyCollection<WaveePlayerPlaybackContextPage>> InitializePages();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO release managed resources here
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public abstract Task<SpotifyId?> GetTrackId(string mediaItemUid);
}

/// <summary>
/// Represents a stateless page of a playback context.
/// </summary>
public class WaveePlayerPlaybackContextPage
{
    private readonly ISpotifyApiClient _api;
    private readonly ContextPage _page;
    private readonly AsyncLock _initializationLock = new();
    private readonly List<WaveePlayerMediaItem> _items = new();
    private bool _initialized;
    private readonly ILogger<IWaveePlayer> _logger;
    private Dictionary<string, string> _metadata = new();
    private int _pageIndex;

    public WaveePlayerPlaybackContextPage(int index, ContextPage page, ISpotifyApiClient api,
        ILogger<IWaveePlayer> logger)
    {
        _pageIndex = index;
        _page = page;
        _api = api;
        _logger = logger;
        _initialized = false;
        foreach (var item in page.Metadata)
        {
            _metadata[item.Key] = item.Value;
        }

        if (page.Tracks is not null && page.Tracks.Count > 0)
        {
            _items.AddRange(page.Tracks.Select(CreateMediaItem));
            _initialized = true;
        }
    }

    public int PageIndex => _pageIndex;
    public string PageUrl => _page.PageUrl;
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    public bool PeekTracks(out IReadOnlyList<WaveePlayerMediaItem>? tracks)
    {
        if (!_initialized)
        {
            tracks = null;
            return false;
        }

        tracks = _items;
        return true;
    }

    public async Task<IReadOnlyList<WaveePlayerMediaItem>> GetTracks()
    {
        await Initialize(CancellationToken.None);
        return _items;
    }

    /// <summary>
    /// Retrieves the media item at the specified index within the playback context page.
    /// </summary>
    /// <param name="index">The index of the media item to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous operation, containing the media item if found.</returns>
    public async ValueTask<WaveePlayerMediaItem?> GetItemAt(int index, CancellationToken cancellationToken)
    {
        // Implementation
        await Initialize(cancellationToken);
        if (index < 0 || index >= _items.Count)
            return null;
        return _items[index];
    }


    private ValueTask Initialize(CancellationToken cancellationToken)
    {
        using (_initializationLock.Lock(cancellationToken))
        {
            if (_initialized)
                return ValueTask.CompletedTask;

            return new ValueTask(InitializeAsync(cancellationToken));
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using (await _initializationLock.LockAsync(cancellationToken))
        {
            if (_initialized)
                return;

            // Implementation
            if (_page.Tracks is not null && _page.Tracks.Count > 0)
            {
                _logger.LogDebug("Initializing context page with {TrackCount} tracks.", _page.Tracks.Count);
                _items.Clear();
                _items.AddRange(_page.Tracks.Select(CreateMediaItem));
                _initialized = true;
                return;
            }

            if (_page.HasPageUrl && !string.IsNullOrEmpty(_page.PageUrl))
            {
                _logger.LogDebug("Fetching context page from URL: {PageUrl}", _page.PageUrl);
                var contextPage = await _api.FetchContextPageAsync(_page.PageUrl, cancellationToken);
                _items.Clear();
                _metadata.Clear();
                foreach (var item in contextPage.Metadata)
                {
                    _metadata[item.Key] = item.Value;
                }

                _logger.LogDebug("Initializing context page with {TrackCount} tracks.", contextPage.Tracks.Count);
                _items.AddRange(contextPage.Tracks.Select(CreateMediaItem));
                _initialized = true;
                return;
            }
            else
            {
                _logger.LogWarning("Context page is empty... WE should really fix this ?!");
                Debugger.Break();
                throw new NotImplementedException();
            }
        }
    }

    private WaveePlayerMediaItem CreateMediaItem(ContextTrack track)
    {
        string? uid = track.Uid;
        SpotifyId id = default;
        if (!string.IsNullOrEmpty(track.Uri))
        {
            id = SpotifyId.FromUri(track.Uri);
        }
        else if (!track.Gid.IsEmpty)
        {
            id = SpotifyId.FromGid(track.Gid, SpotifyItemType.Track);
        }

        var metadata = new Dictionary<string, string>();
        foreach (var item in track.Metadata)
        {
            metadata[item.Key] = item.Value;
        }

        return new WaveePlayerMediaItem(id, uid, metadata);
    }

    internal void SortItems(List<SortDescriptor> sortDescriptors)
    {
        using (_initializationLock.Lock())
        {
            if (!_initialized)
                return;

            SortHelper.SortTracks(_items, sortDescriptors);
        }
    }
}