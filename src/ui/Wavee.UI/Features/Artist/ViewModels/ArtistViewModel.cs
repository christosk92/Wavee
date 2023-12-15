using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using LiteDB;
using Mediator;
using Nito.AsyncEx;
using Spotify.Metadata;
using Wavee.Domain.Playback;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Playback;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Features.Playback;
using Wavee.UI.Features.Playback.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Test;
using ICommand = System.Windows.Input.ICommand;

namespace Wavee.UI.Features.Artist.ViewModels;

public sealed class ArtistViewModel : NavigationItemViewModel, IPlaybackChangedListener
{
    private readonly IMediator _mediator;
    private readonly string _id;
    private bool _initialized;
    private string _name;
    private string _monthlyListeners;
    private string? _headerImageUrl;
    private string? _profilePictureImageUrl;
    private SimpleAlbumEntity _latestRelease;
    public ArtistViewModel(IMediator mediator, 
        string id, 
        IUIDispatcher dispatcher, 
        PlaybackViewModel playback,
        PlaylistsNavItem playlistsNavItem, 
        RightSidebarViewModel rightSidebar)
    {
        _mediator = mediator;
        _id = id;
        PlaylistsNavItem = playlistsNavItem;
        RightSidebar = rightSidebar;
        Children = new NavigationItemViewModel[]
        {
            new ArtistOverviewViewModel(mediator, id, dispatcher, playback),
            new ArtistRelatedContentViewModel(this),
            new ArtistAboutViewModel(this)
        };
        PlayCommand = new AsyncRelayCommand<object>(async x =>
        {
            try
            {
                var playbackVm = playback;
                switch (x)
                {
                    case ArtistTopTrackViewModel topTrack:
                        var ctx = PlayContext.FromArtist(this, true)
                            .FromTopTracks(Overview.TopTracks)
                            .StartWithTrack(topTrack)
                            .Build();
                        await playbackVm.Play(ctx);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public PlaylistsNavItem PlaylistsNavItem { get; }
    public RightSidebarViewModel RightSidebar { get; }

    public AsyncRelayCommand<object> PlayCommand { get;  }

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
        Overview.Initialize(artist, PlayCommand);

        _initialized = true;
    }

    private static string Format(ulong artistMonthlyListeners)
    {
        return artistMonthlyListeners.ToString("N0", CultureInfo.InvariantCulture);
    }
    public ArtistOverviewViewModel Overview => (ArtistOverviewViewModel)Children[0];
    public ArtistRelatedContentViewModel RelatedContent => (ArtistRelatedContentViewModel)Children[1];
    public ArtistAboutViewModel About => (ArtistAboutViewModel)Children[2];
    public string Id => _id;
    public void OnPlaybackChanged(PlaybackViewModel player)
    {
        Overview.OnPlaybackChanged(player);
    }
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


public sealed class ArtistTopTrackViewModel : ObservableObject
{
    private WaveeTrackPlaybackState _playbackState;
    public required ArtistTopTrackEntity Track { get; init; }
    public required int Number { get; init; }
    public required string Playcount { get; init; }
    public required string Duration { get; init; }
    public required ICommand PlayCommand { get; set; }
    public object This => this;

    public WaveeTrackPlaybackState PlaybackState
    {
        get => _playbackState;
        set => SetProperty(ref _playbackState, value);
    }
}