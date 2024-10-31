using Eum.Spotify.context;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Playback.Contexts;

internal sealed class WaveeRegularPlaybackContext : WaveePlayerPlaybackContext
{
    private readonly string _contextId;
    private readonly ISpotifyApiClient _api;
    private readonly List<WaveePlayerPlaybackContextPage> _pages = new();
    private readonly Dictionary<string, string> _contextMetadata = new();
    private readonly Task _initializationTask;

    private string? _contextUrl;
    private Restrictions? _contextRestrictions;
    private readonly ILogger<IWaveePlayer> _logger;


    public WaveeRegularPlaybackContext(string contextId, ISpotifyApiClient api, ILogger<IWaveePlayer> logger)
    {
        _contextId = contextId;
        _api = api;
        _logger = logger;
        _initializationTask = Initialize(CancellationToken.None);
    }

    public IReadOnlyDictionary<string, string> Metadata => _contextMetadata;
    public string? ContextUrl => _contextUrl;
    public Restrictions? ContextRestrictions => _contextRestrictions;
    public string ContextId => _contextId;

    private readonly AsyncLock _initializationLock = new();

    private async Task Initialize(CancellationToken cancellationToken)
    {
        using (await _initializationLock.LockAsync(cancellationToken))
        {
            _logger.LogDebug("Initializing Wavee regular playback context {ContextId}.", _contextId);

            var context = await _api.ResolveContext(_contextId, cancellationToken);
            _contextMetadata.Clear();
            _pages.Clear();
            foreach (var item in context.Metadata)
            {
                _contextMetadata[item.Key] = item.Value;
            }

            _contextUrl = context.Url;
            _contextRestrictions = context.Restrictions;

            _logger.LogDebug("Wavee regular playback context {ContextId} initialized.", _contextId);
            for (var index = 0; index < context.Pages.Count; index++)
            {
                var page = context.Pages[index];
                _pages.Add(new WaveePlayerPlaybackContextPage(index, page, _api, _logger));
            }
        }
    }

    public override async Task<WaveePlayerPlaybackContextPage?> GetPage(
        int pageIndex,
        CancellationToken cancellationToken)
    {
        await _initializationTask;
        if (pageIndex < 0 || pageIndex >= _pages.Count)
            return null;
        var page = _pages[pageIndex];
        return page;
    }

    public override string Id => _contextId;

    public override async Task<IReadOnlyCollection<WaveePlayerPlaybackContextPage>> InitializePages()
    {
        await _initializationTask;
        return _pages.ToList();
    }

    public override async Task<SpotifyId?> GetTrackId(string mediaItemUid)
    {
        // This should not be necessary for regular playback contexts
        foreach (var page in _pages)
        {
            var tracks = await page.GetTracks();
            foreach(var track in tracks)
            {
                if (track.Uid == mediaItemUid)
                {
                    return track.Id;
                }
            }
        }

        return null;
    }

    public void LoadPages(RepeatedField<ContextPage> pages)
    {
        using (_initializationLock.Lock())
        {
            _pages.Clear();
            for (var index = 0; index < pages.Count; index++)
            {
                var page = pages[index];
                _pages.Add(new WaveePlayerPlaybackContextPage(index, page, _api, _logger));
            }
        }
    }
}