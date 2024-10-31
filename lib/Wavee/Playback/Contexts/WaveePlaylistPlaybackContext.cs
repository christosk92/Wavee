using Eum.Spotify.context;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Wavee.Helpers;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Models.Metadata;
using Wavee.Models.Playlist;
using Wavee.Playback.Player;

namespace Wavee.Playback.Contexts;

public sealed class WaveePlaylistPlaybackContext : WaveePlayerPlaybackContext
{
    private readonly ISpotifyPlaylistClient _playlistClient;
    private readonly ISpotifyApiClient _apiClient;
    private readonly ILogger<IWaveePlayer> _logger;
    private readonly List<WaveePlayerPlaybackContextPage> _pages = new();
    private readonly Task _initializationTask;
    private SpotifyPlaylist? _playlist;

    public WaveePlaylistPlaybackContext(string contextUri,
        ISpotifyPlaylistClient playlistClient,
        ISpotifyApiClient apiClient, ILogger<IWaveePlayer> createLogger)
    {
        _playlistClient = playlistClient;
        _apiClient = apiClient;
        _logger = createLogger;
        Id = contextUri;
        _initializationTask = Initialize(CancellationToken.None);
    }


    public override string Id { get; }

    public override async Task<WaveePlayerPlaybackContextPage?> GetPage(int pageIndex,
        CancellationToken cancellationToken)
    {
        await _initializationTask;

        if (pageIndex < 0 || pageIndex >= _pages.Count)
        {
            return null;
        }

        return _pages[pageIndex];
    }

    public override async Task<IReadOnlyCollection<WaveePlayerPlaybackContextPage>> InitializePages()
    {
        await _initializationTask;
        return _pages.ToList();
    }

    private async Task Initialize(CancellationToken cancellationToken)
    {
        try
        {
            _playlist = await _playlistClient.GetPlaylist(SpotifyId.FromUri(Id), cancellationToken);
            if (_playlist == null)
            {
                _logger.LogWarning("Failed to retrieve playlist {PlaylistId}", Id);
                return;
            }

            await _playlist.InitializeTracksData(cancellationToken);
            var ctxPage = new ContextPage();
            foreach (var track in _playlist.Tracks)
            {
                var ctxTrack = new ContextTrack
                {
                    Uri = track.Id.ToString(),
                };
                if (!string.IsNullOrEmpty(track.Uid))
                {
                    ctxTrack.Uid = track.Uid;
                }

                ctxTrack.Gid = ByteString.CopyFrom(track.Id.ToRaw());
                ctxPage.Tracks.Add(ctxTrack);
            }

            var newPage = new WaveePlayerPlaybackContextPage(0, ctxPage, _apiClient, _logger);
            if (!newPage.PeekTracks(out var t))
            {
                throw new InvalidOperationException("Failed to peek tracks from the new page.");
            }

            foreach (var contetTrack in t)
            {
                var playlistTrack = _playlist.Tracks.FirstOrDefault(x => x.Id == contetTrack.Id);
                if (playlistTrack.Item is SpotifyTrack track)
                {
                    contetTrack.AddIds(track.Id);
                    contetTrack.AddIds(track.AlternativeIds.ToArray());
                    // contetTrack.UpdateMetadata(track,
                    //     playlistTrack.OriginalIndex,
                    //     playlistTrack.AddedAt ?? DateTimeOffset.MinValue);MinValue
                }
            }

            _pages.Add(newPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize playlist {PlaylistId}", Id);
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _playlist?.Dispose();
        base.Dispose(disposing);
    }

    public override async Task<SpotifyId?> GetTrackId(string mediaItemUid)
    {
        foreach (var page in _pages)
        {
            var tracks = await page.GetTracks();
            foreach (var track in tracks)
            {
                if (track.Uid == mediaItemUid)
                {
                    return track.Id;
                }
            }
        }

        return null;
    }

    public Task UpdateSortingCriteria(string? sortingCriteria)
    {
        return Task.CompletedTask;
    }
}