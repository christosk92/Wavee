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
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Artist.ViewModels;

public sealed class ArtistViewModel : NavigationItemViewModel
{
    private Dictionary<DiscographyGroupType, List<ArtistViewDiscographyItemViewModel>> _discographyCache = new();
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

        /*
         * group.Items.Select((x, i) => new ArtistViewDiscographyItemViewModel
           {
           Entity = x.Album,
           Tracks = BuildTracks(x),
           Number = (i+ j1).ToString(CultureInfo.InvariantCulture)
           }).ToImmutableArray()
         */
        foreach (var group in artist.Discography
                     .Where(x => x is { Type: not DiscographyGroupType.PopularRelease, Items: { Count: > 0 } }))
        {
            var newGroup = new ArtistViewDiscographyGroupViewModel
            {
                ArtistId = _id,
                Title = group.Type.ToString(),
                Items = new ObservableCollection<ArtistViewDiscographyItemViewModel>(),
                Mediator = _mediator,
                TotalItems = group.Total,
                Type = group.Type
            };
            Discography.Add(newGroup);
            _discographyCache[group.Type] = new List<ArtistViewDiscographyItemViewModel>();
            foreach (var item in group.Items)
            {
                if (item.HasValue)
                {
                    var vm = BuildVm(item.Album!);
                    // var vm = new ArtistViewDiscographyItemViewModel
                    // {
                    //     Entity = item.Album!,
                    //     Tracks = new ObservableCollection<ArtistViewDiscographyTrackViewModel>(),
                    //     Group = group.Type
                    // };
                    _discographyCache[group.Type].Add(vm);
                }
            }
        }
        _initialized = true;
    }

    private ArtistViewDiscographyItemViewModel BuildVm(SimpleAlbumEntity album)
    {
        var vm = new ArtistViewDiscographyItemViewModel
        {
            Tracks = new ObservableCollection<ArtistViewDiscographyTrackViewModel>(),
            Group = album.Type?.ToLower() switch
            {
                "single" or "ep" => DiscographyGroupType.Single,
                "album" => DiscographyGroupType.Album,
                _ => DiscographyGroupType.Compilation
            },
            Id = album.Id,
            Name = album.Name,
            MediumImageUrl = album.Images.OrderBy(x => x.Height).Skip(1).FirstOrDefault().Url ?? album.Images
                .FirstOrDefault().Url,
            Year = album.Year?.ToString(CultureInfo.InvariantCulture) ?? "Unknown"
        };

        return vm;
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

    private AsyncLock _lock = new();
    private ArtistViewDiscographyGroupViewModel? currentGroup;

    public async Task FetchNextDiscography(bool initial)
    {
        using (await _lock.LockAsync())
        {
            try
            {
                if (currentGroup is null)
                {
                    currentGroup = Discography.FirstOrDefault();
                }

                if (currentGroup is null)
                {
                    return;
                }

                var group = currentGroup;
                var totalItems = group.TotalItems;
                var alreadyAddedItems = group.Items.Count;
                var cachedItems = _discographyCache[group.Type]
                    .Skip(alreadyAddedItems)
                    .ToArray();
                var totalFutureItems = cachedItems.Length + alreadyAddedItems;
                //Check if we need to fetch more items from the API. 
                var needToFetch = totalFutureItems < totalItems;
                if (needToFetch && !initial)
                {
                    // Offset will be the total items before this group + the items we already have in this group.

                    //Check if we actually need to fetch these
                    if (alreadyAddedItems < totalItems)
                    {
                        var results = await _mediator.Send(new GetAlbumsForArtistQuery()
                        {
                            Id = _id,
                            Offset = alreadyAddedItems + cachedItems.Length,
                            Limit = 20,
                            FetchTracks = false,
                            Group = group.Type
                        });
                        foreach (var album in results.Albums)
                        {
                            var vm = BuildVm(album);
                            _discographyCache[vm.Group].Add(vm);
                        }
                    }

                    cachedItems = _discographyCache[group.Type]
                        .Skip(alreadyAddedItems)
                        .ToArray();
                    foreach (var item in cachedItems)
                    {
                        group.Items.Add(item);
                    }


                    totalFutureItems = group.Items.Count;
                }
                else
                {
                    // We have all the items we need, so we can just add them to the group.
                    foreach (var item in cachedItems)
                    {
                        group.Items.Add(item);
                    }
                }

                //If we have all the items we need, we can move to the next group.
                if (totalFutureItems >= totalItems)
                {
                    var index = Discography.IndexOf(group);
                    if (index < Discography.Count - 1)
                    {
                        currentGroup = Discography[index + 1];
                    }
                    else
                    {
                        // Do nothing
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}

public sealed class ArtistViewDiscographyGroupViewModel
{
    public string ArtistId { get; set; }
    public string Title { get; set; }
    public ObservableCollection<ArtistViewDiscographyItemViewModel> Items { get; set; }
    public IMediator Mediator { get; set; }
    public uint TotalItems { get; set; }
    public DiscographyGroupType Type { get; set; }
}

public sealed class ArtistViewDiscographyItemViewModel : ObservableObject
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string MediumImageUrl { get; set; }
    public required string Year { get; set; }
    public required ObservableCollection<ArtistViewDiscographyTrackViewModel> Tracks { get; init; }
    public required DiscographyGroupType Group { get; init; }
}

public sealed class ArtistViewDiscographyTrackViewModel : ObservableObject
{
    private AlbumTrackEntity? _track;
    private bool _HasValue;

    public required AlbumTrackEntity? Track
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