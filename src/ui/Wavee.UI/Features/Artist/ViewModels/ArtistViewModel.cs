using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using LiteDB;
using Mediator;
using Nito.AsyncEx;
using Spotify.Metadata;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Test;

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

    public ArtistViewModel(IMediator mediator, string id, IUIDispatcher dispatcher)
    {
        _mediator = mediator;
        _id = id;
        Children = new NavigationItemViewModel[]
        {
            new ArtistOverviewViewModel(mediator, id, dispatcher),
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
        Overview.Initialize(artist);

        _initialized = true;
    }

    private static string Format(ulong artistMonthlyListeners)
    {
        return artistMonthlyListeners.ToString("N0", CultureInfo.InvariantCulture);
    }
    public ArtistOverviewViewModel Overview => (ArtistOverviewViewModel)Children[0];
    public ArtistRelatedContentViewModel RelatedContent => (ArtistRelatedContentViewModel)Children[1];
    public ArtistAboutViewModel About => (ArtistAboutViewModel)Children[2];
}


public sealed class ArtistViewDiscographyGroupViewModel : ObservableObject
{
    private int _selectedIndex;
    public string ArtistId { get; set; }
    public string Title { get; set; }
    public ObservableCollection<LazyArtistViewDiscographyItemViewModel> Items { get; set; }
    public IMediator Mediator { get; set; }
    public uint TotalItems { get; set; }
    public DiscographyGroupType Type { get; set; }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, value);
    }
}
public sealed class LazyArtistViewDiscographyItemViewModel : ObservableObject
{
    private ArtistViewDiscographyItemViewModel? _value;
    private bool _hasvalue;
    public required Func<int, int, DiscographyGroupType, Task> StartFetchingFunc { get; init; }

    public required DiscographyGroupType Type { get; init; }
    public required int BatchNumber { get; init; }
    public required int Number { get; init; }
    public ArtistViewDiscographyItemViewModel? Value
    {
        get
        {
            if (_value is null)
            {
                Task.Run(async () => await StartFetchingFunc(BatchNumber, Number, Type));
            }

            return _value;
        }
        set
        {
            SetProperty(ref _value, value);
            HasValue = value is not null;
        }
    }

    public bool HasValue
    {
        get => _hasvalue;
        set => SetProperty(ref _hasvalue, value);
    }
}
public sealed class ArtistViewDiscographyItemViewModel : ObservableObject
{
    public AlbumViewModel Album { get; init; }
    public required DiscographyGroupType Group { get; init; }
}


public sealed class ArtistTopTrackViewModel
{
    public required ArtistTopTrackEntity Track { get; init; }
    public required int Number { get; init; }
    public required string Playcount { get; init; }
    public required string Duration { get; init; }
}