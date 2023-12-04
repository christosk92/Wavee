using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Artist.ViewModels;

public sealed class ArtistViewModel : NavigationItemViewModel
{
    private readonly IMediator _mediator;
    private readonly string _id;
    private bool _initialized;
    private string _name;
    private string _monthlyListeners;
    private string? _headerImageUrl;
    private string? _profilePictureImageUrl;
    private SimpleAlbumEntity _latestRelease;

    public ArtistViewModel(IMediator mediator, string id)
    {
        _mediator = mediator;
        _id = id;
        Children = new NavigationItemViewModel[]
        {
            new ArtistOverviewViewModel(this),
            new ArtistRelatedContentViewModel(this),
            new ArtistAboutViewModel(this)
        };
    }

    public override NavigationItemViewModel[] Children { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string MonthlyListeners
    {
        get => _monthlyListeners;
        set => SetProperty(ref _monthlyListeners, value);
    }

    public string? HeaderImageUrl
    {
        get => _headerImageUrl;
        set => SetProperty(ref _headerImageUrl, value);
    }

    public string? ProfilePictureImageUrl
    {
        get => _profilePictureImageUrl;
        set => SetProperty(ref _profilePictureImageUrl, value);
    }

    public ObservableCollection<ArtistTopTrackViewModel> TopTracks { get; } = new();
    public ObservableCollection<ArtistViewDiscographyGroupViewModel> Discography { get; } = new();

    public SimpleAlbumEntity? LatestRelease
    {
        get => _latestRelease;
        set => SetProperty(ref _latestRelease, value);
    }

    public async Task Initialize()
    {
        if (_initialized) return;

        var artist = await _mediator.Send(new GetArtistViewQuery
        {
            Id = _id
        });

        Name = artist.Name;
        MonthlyListeners = Format(artist.MonthlyListeners);
        HeaderImageUrl = artist.HeaderImageUrl;
        ProfilePictureImageUrl = artist.ProfilePictureImageUrl;
        int i = 1;
        foreach (var track in artist.TopTracks)
        {
            TopTracks.Add(new ArtistTopTrackViewModel
            {
                Track = track,
                Number = i,
                Playcount = track.Playcount.HasValue ? Format(track.Playcount.Value) : "< 1,000",
                Duration = FormatDuration(track.Duration)
            });
            i++;
        }

        foreach (var group in artist.Discography
                     .Where(x => x is { Type: not DiscographyGroupType.PopularRelease, Items: { Count: > 0 } }))
        {
            Discography.Add(new ArtistViewDiscographyGroupViewModel
            {
                Title = group.Type.ToString(),
                Items = group.Items.Select(x => new ArtistViewDiscographyItemViewModel
                {
                    Entity = x.Album,
                    Tracks = BuildTracks(x)
                }).ToImmutableArray()
            });
        }

        _initialized = true;
    }

    private string FormatDuration(TimeSpan trackDuration)
    {
        var totalHours = (int)trackDuration.TotalHours;
        if (totalHours > 0)
        {
            return trackDuration.ToString(@"hh\:mm\:ss");
        }
        else
        {
            return trackDuration.ToString(@"mm\:ss");
        }
    }

    private ObservableCollection<ArtistViewDiscographyTrackViewModel> BuildTracks(ArtistViewDiscographyItem artistViewDiscographyItem)
    {
        if (artistViewDiscographyItem.HasValue)
        {
            var tracksCount = artistViewDiscographyItem.Album!.TracksCount!.Value;
            var emptyTracks = new ArtistViewDiscographyTrackViewModel[tracksCount];
            for (int i = 0; i < tracksCount; i++)
            {
                emptyTracks[i] = new ArtistViewDiscographyTrackViewModel
                {
                    Track = null,
                    Number = i + 1
                };
            }

            return new ObservableCollection<ArtistViewDiscographyTrackViewModel>(emptyTracks);
        }

        return new ObservableCollection<ArtistViewDiscographyTrackViewModel>();
    }

    private static string Format(ulong artistMonthlyListeners)
    {
        return artistMonthlyListeners.ToString("N0", CultureInfo.InvariantCulture);
    }
}

public sealed class ArtistViewDiscographyGroupViewModel
{
    public required string Title { get; init; }
    public required IReadOnlyCollection<ArtistViewDiscographyItemViewModel> Items { get; init; }
}

public sealed class ArtistViewDiscographyItemViewModel : ObservableObject
{
    private SimpleAlbumEntity _entity;
    private bool _hasValue;
    public required SimpleAlbumEntity? Entity
    {
        get => _entity;
        set
        {
            SetProperty(ref _entity, value);
            HasValue = value is not null;
        }
    }

    public bool HasValue
    {
        get => _hasValue;
        set => SetProperty(ref _hasValue, value);
    }

    public required ObservableCollection<ArtistViewDiscographyTrackViewModel> Tracks { get; init; } = new();
}

public sealed class ArtistViewDiscographyTrackViewModel : ObservableObject
{
    private ArtistAlbumTrackEntity? _track;
    private bool _HasValue;

    public required ArtistAlbumTrackEntity? Track
    {
        get => _track;
        set
        {
            SetProperty(ref _track, value);
            HasValue = value is not null;
        }
    }

    public bool HasValue
    {
        get => _HasValue;
        set => SetProperty(ref _HasValue, value);
    }

    public required int Number { get; init; }
}

public sealed class ArtistTopTrackViewModel
{
    public required ArtistTopTrackEntity Track { get; init; }
    public required int Number { get; init; }
    public required string Playcount { get; init; }
    public required string Duration { get; init; }
}