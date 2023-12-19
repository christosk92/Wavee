using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mediator;
using System.Xml.Linq;
using Spotify.Metadata;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Features.Album.Queries;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Playback.ViewModels;
using System.Text;

namespace Wavee.UI.Features.Album.ViewModels;

public sealed class AlbumViewViewModel : ObservableObject
{
    private readonly string _id;
    private readonly IMediator _mediator;

    private bool _initialized = false;

    private string? _name;
    private DateOnly? _releaseDate;
    private IReadOnlyCollection<SimpleArtistEntity>? _artists;
    private string? _largeImageUrl;
    private string? _mediumImageUrl;

    private IReadOnlyCollection<AlbumDiscViewModel>? _discs;
    private IReadOnlyCollection<SimpleAlbumEntity>? moreAlbumsByArtist;

    private IReadOnlyCollection<Copyright>? _copyrights;
    private string? _label;

    private bool _loaded;
    private TimeSpan? _totalDuration;
    private uint _tracksCount;
    private string? _totalDurationString;
    private string? _releaseDateString;

    public AlbumViewViewModel(string id, IMediator mediator, PlaybackViewModel playbackViewModel)
    {
        _id = id;
        _mediator = mediator;
    }

    public bool Loaded
    {
        get => _loaded;
        set => SetProperty(ref _loaded, value);
    }

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public DateOnly? ReleaseDate
    {
        get => _releaseDate;
        set => SetProperty(ref _releaseDate, value);
    }

    public IReadOnlyCollection<SimpleArtistEntity>? Artists
    {
        get => _artists;
        set => SetProperty(ref _artists, value);
    }

    public string? LargeImageUrl
    {
        get => _largeImageUrl;
        set => SetProperty(ref _largeImageUrl, value);
    }

    public string? MediumImageUrl
    {
        get => _mediumImageUrl;
        set => SetProperty(ref _mediumImageUrl, value);
    }

    public IReadOnlyCollection<AlbumDiscViewModel>? Discs
    {
        get => _discs;
        set => SetProperty(ref _discs, value);
    }

    public IReadOnlyCollection<SimpleAlbumEntity>? MoreAlbumsByArtist
    {
        get => moreAlbumsByArtist;
        set => SetProperty(ref moreAlbumsByArtist, value);
    }

    public IReadOnlyCollection<Copyright>? Copyrights
    {
        get => _copyrights;
        set => SetProperty(ref _copyrights, value);
    }

    public string? Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public IReadOnlyCollection<int> NoTracks { get; } = Enumerable.Range(1, 10).Select(x => (int)x).ToImmutableArray();

    public TimeSpan? TotalDuration
    {
        get => _totalDuration;
        set => SetProperty(ref _totalDuration, value);
    }

    public uint TracksCount
    {
        get => _tracksCount;
        set => SetProperty(ref _tracksCount, value);
    }

    public string? TotalDurationString
    {
        get => _totalDurationString;
        set => SetProperty(ref _totalDurationString, value);
    }

    public string? ReleaseDateString
    {
        get => _releaseDateString;
        set => SetProperty(ref _releaseDateString, value);
    }


    public async Task Initialize()
    {
        if (_initialized) return;

        var album = await _mediator.Send(new GetAlbumViewQuery
        {
            Id = _id
        });

        Name = album.Name;
        ReleaseDate = album.ReleaseDate;
        Artists = album.Artists;
        LargeImageUrl = album.LargeImageUrl;
        MediumImageUrl = album.MediumImageUrl;
        Discs = album.Discs.Select(x => new AlbumDiscViewModel
        {
            Number = x.Number,
            Tracks = x.Tracks.Select((f, i) => new AlbumTrackViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Duration = f.Duration,
                Number = i + 1,
                PlayCommand = null,
                Album = null,
                Playcount = f.PlayCount
            }).ToImmutableArray()
        }).ToImmutableArray();
        MoreAlbumsByArtist = album.MoreAlbumsByArtist;
        Label = album.Label;
        Copyrights = album.Copyrights;

        var dur = TimeSpan.Zero;
        foreach (var disc in album.Discs)
        {
            foreach (var track in disc.Tracks)
            {
                TracksCount++;
                dur += track.Duration;
            }
        }

        TotalDuration = dur;
        Loaded = true;

        TotalDurationString = FormatTime(TotalDuration);
        ReleaseDateString = ReleaseDateStr(ReleaseDate);



        _initialized = true;
    }


    private static string FormatTime(TimeSpan? timeSpan)
    {
        if (timeSpan is null)
        {
            return "--";
        }

        // 01:24:30 -> 1 hr 24 min 30 sec
        var sb = new StringBuilder();

        if (timeSpan.Value.TotalHours >= 1)
        {
            sb.Append($"{(int)timeSpan.Value.TotalHours} hr ");
        }

        if (timeSpan.Value.Minutes > 0)
        {
            sb.Append($"{timeSpan.Value.Minutes} min ");
        }

        if (timeSpan.Value.Seconds > 0)
        {
            sb.Append($"{timeSpan.Value.Seconds} sec");
        }

        return sb.ToString();
    }

    private static string ReleaseDateStr(DateOnly? dateOnly)
    {
        return dateOnly?.Year.ToString() ?? "--";
    }
}

public sealed class AlbumDiscViewModel
{
    public int Number { get; init; }
    public IReadOnlyCollection<AlbumTrackViewModel>? Tracks { get; init; }
}