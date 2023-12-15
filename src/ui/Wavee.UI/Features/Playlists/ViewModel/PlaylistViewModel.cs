using System.Collections.Immutable;
using System.Collections.ObjectModel;
using AngleSharp.Dom;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Common;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Features.Playlists.Queries;
using Wavee.UI.Features.Tracks;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Playlists.ViewModel;

public sealed class PlaylistViewModel : ObservableObject
{
    private string? _title;
    private string? _bigImage;
    private bool _tracksLoaded;
    private bool _dataLoaded;
    private string? _description;
    private ulong? _popCount;
    private TimeSpan? _totalDuration;
    private readonly IMediator _mediator;
    private readonly IUIDispatcher _uiDispatcher;
    public PlaylistViewModel(PlaylistSidebarItemViewModel sidebarItem, IMediator mediator, IUIDispatcher uiDispatcher)
    {
        _mediator = mediator;
        _uiDispatcher = uiDispatcher;
        Id = sidebarItem.Id;
        Title = sidebarItem.Name;
        BigImage = sidebarItem.BigImage;

        for (int i = 0; i < sidebarItem.Items; i++)
        {
            Tracks.Add(new LazyPlaylistTrackViewModel
            {
                HasValue = false,
                Track = null,
                Index = i
            });
        }
        Description = sidebarItem.Description;

        TracksLoaded = false;
        DataLoaded = true;
    }

    public string Id { get; }
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    public string? BigImage
    {
        get => _bigImage;
        set => SetProperty(ref _bigImage, value);
    }

    public bool DataLoaded
    {
        get => _dataLoaded;
        set => SetProperty(ref _dataLoaded, value);
    }
    public bool TracksLoaded
    {
        get => _tracksLoaded;
        set => SetProperty(ref _tracksLoaded, value);
    }

    public void Initialize(CancellationToken cancellationToken)
    {

        // PopCount
        _ = Task.Run(async () => await _mediator.Send(new GetPlaylistSavedCountQuery
        {
            PlaylistId = Id
        }), cancellationToken).ContinueWith(x =>
        {
            _uiDispatcher.Invoke(() =>
            {
                PopCount = x.Result;
            });
        }, cancellationToken);

        // Tracks 
        _ = Task.Run(async () => await FetchAndSetTracks(), cancellationToken);
    }

    private async Task FetchAndSetTracks()
    {
        var trackIdsAndAttributes = await _mediator.Send(new GetPlaylistTracksIdsQuery
        {
            PlaylistId = Id
        });

        var tracksMetadata = await _mediator.Send(new GetTracksMetadataRequest
        {
            Ids = trackIdsAndAttributes.Select(x => x.Id).ToImmutableArray()
        });

        _uiDispatcher.Invoke(() =>
        {
            Tracks.Clear();
            int index = 0;
            TracksLoaded = true;
            double totalSeconds = 0;
            foreach (var (track, info) in tracksMetadata.Zip(trackIdsAndAttributes))
            {
                if (track.Value.HasValue)
                {
                    PlaylistTrackViewModel? trackasVm = null;
                    if (track.Value.Value.Track is not null)
                    {
                        trackasVm = new PlaylistTrackViewModel(track.Value.Value.Track, info);
                        totalSeconds += trackasVm.Duration.TotalSeconds;
                    }
                    Tracks.Add(new LazyPlaylistTrackViewModel
                    {
                        HasValue = true,
                        Track = trackasVm!,
                        Index = index++
                    });
                }
                else
                {
                    Tracks.Add(new LazyPlaylistTrackViewModel
                    {
                        HasValue = false,
                        Track = null,
                        Index = index++,
                    });
                }
            }

            TotalDuration = TimeSpan.FromSeconds(totalSeconds);
        });
    }

    public ObservableCollection<LazyPlaylistTrackViewModel> Tracks { get; } = new();

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public ulong? PopCount
    {
        get => _popCount;
        set => SetProperty(ref _popCount, value);
    }

    public TimeSpan? TotalDuration
    {
        get => _totalDuration;
        set => SetProperty(ref _totalDuration, value);
    }
}

public sealed class LazyPlaylistTrackViewModel : ObservableObject
{
    private bool _hasValue;
    private PlaylistTrackViewModel? _track;

    public bool HasValue
    {
        get => _hasValue;
        set => SetProperty(ref _hasValue, value);
    }

    public PlaylistTrackViewModel? Track
    {
        get => _track;
        set => SetProperty(ref _track, value);
    }

    public required int Index { get; init; }

    public int AddOne(int i)
    {
        return i + 1;
    }

    public string FormatDuration(TimeSpan timeSpan)
    {
        var totalHours = (int)timeSpan.TotalHours;
        if (totalHours > 0)
        {
            //=> Duration.ToString(@"mm\:ss");
            return timeSpan.ToString(@"hh\:mm\:ss");
        }
        else
        {
            return timeSpan.ToString(@"mm\:ss");
        }
    }
}

public sealed class PlaylistTrackViewModel
{
    public PlaylistTrackViewModel(Track spotifyTrack, PlaylistTrackInfo trackInfo)
    {
        Name = spotifyTrack.Name;

        const string url = "https://i.scdn.co/image/";
        SmallestImageUrl = spotifyTrack.Album.CoverGroup.Image.Select(c =>
        {
            var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
            var hex = id.ToBase16();

            return ($"{url}{hex}", c.Width);
        }).OrderBy(x => x.Width).FirstOrDefault().Item1;

        Artists = spotifyTrack.Artist.Select(x => (SpotifyId.FromRaw(x.Gid.Span, SpotifyItemType.Artist).ToString(), x.Name))
            .ToImmutableArray();
        Album = (SpotifyId.FromRaw(spotifyTrack.Album.Gid.Span, SpotifyItemType.Album).ToString(),
            spotifyTrack.Album.Name);
        Duration = TimeSpan.FromMilliseconds(spotifyTrack.Duration);

        AddedAt = trackInfo.AddedAt;
        AddedBy = trackInfo.AddedBy;
        UniquePlaylistItemId = trackInfo.UniqueItemId;
    }

    public string Name { get; }
    public string? SmallestImageUrl { get; }
    public IReadOnlyCollection<(string Id, string Name)> Artists { get; }
    public (string Id, string Name) Album { get; }
    public TimeSpan Duration { get; }

    public DateTimeOffset? AddedAt { get; }
    public string? AddedBy { get; }
    public string? UniquePlaylistItemId { get; }
}