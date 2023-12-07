using System.Collections.ObjectModel;
using System.Globalization;
using Mediator;
using NeoSmart.AsyncLock;
using Wavee.UI.Domain.Album;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Test;

namespace Wavee.UI.Features.Artist.ViewModels;

public sealed class ArtistOverviewViewModel : NavigationItemViewModel
{
    // private Dictionary<DiscographyGroupType, List<ArtistViewDiscographyItemViewModel>> _discographyCache = new();
    private ArtistViewDiscographyGroupViewModel? currentGroup;
    private SimpleAlbumEntity? _latestRelease;
    private AsyncLock _lock = new();
    private readonly IMediator _mediator;
    private readonly string _id;
    private readonly IUIDispatcher _uiDispatcher;
    public ArtistOverviewViewModel(IMediator mediator, string id, IUIDispatcher uiDispatcher)
    {
        _mediator = mediator;
        _id = id;
        _uiDispatcher = uiDispatcher;
    }
    public double ScrollPosition { get; set; }
    public ObservableCollection<ArtistTopTrackViewModel> TopTracks { get; } = new();
    public ObservableCollection<ArtistViewDiscographyGroupViewModel> Discography { get; } = new();

    public SimpleAlbumEntity? LatestRelease
    {
        get => _latestRelease;
        set => SetProperty(ref _latestRelease, value);
    }
    private ObservableCollection<AlbumTrackViewModel> BuildTracks(ArtistViewDiscographyItem artistViewDiscographyItem)
    {
        if (artistViewDiscographyItem.HasValue)
        {
            var tracksCount = artistViewDiscographyItem.Album!.TracksCount!.Value;
            var emptyTracks = new AlbumTrackViewModel[tracksCount];
            for (int i = 0; i < tracksCount; i++)
            {
                emptyTracks[i] = new AlbumTrackViewModel
                {
                    Number = i + 1,
                    Id = null,
                    Name = null,
                    Duration = default
                };
            }

            return new ObservableCollection<AlbumTrackViewModel>(emptyTracks);
        }

        return new ObservableCollection<AlbumTrackViewModel>();
    }
    public void Initialize(ArtistViewResult artist)
    {
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
                Items = new ObservableCollection<LazyArtistViewDiscographyItemViewModel>(),
                Mediator = _mediator,
                TotalItems = group.Total,
                Type = group.Type
            };
            Discography.Add(newGroup);
            int batchNumber = 0;
            const int batchSize = 10;
            int number = 0;
            foreach (var item in group.Items)
            {
                newGroup.Items.Add(new LazyArtistViewDiscographyItemViewModel
                {
                    StartFetchingFunc = StartFetchingFunc,
                    Type = group.Type,
                    BatchNumber = batchNumber,
                    Number = number,
                    Value = item.HasValue ? BuildVm(item.Album!) : null
                });
                number++;
                if (number % batchSize == 0)
                {
                    batchNumber++;
                }

                // if (item.HasValue)
                // {
                //     var vm = BuildVm(item.Album!);
                //     // var vm = new ArtistViewDiscographyItemViewModel
                //     // {
                //     //     Entity = item.Album!,
                //     //     Tracks = new ObservableCollection<ArtistViewDiscographyTrackViewModel>(),
                //     //     Group = group.Type
                //     // };
                //     _discographyCache[group.Type].Add(vm);
                // }
            }
        }
    }
    private Dictionary<DiscographyGroupType, Dictionary<int, ArtistAlbumsResult>> _fetchedBatches = new();

    private async Task StartFetchingFunc(int batchNumber, int _, DiscographyGroupType arg2)
    {
        const int batchSize = 10;

        static void Set(ArtistViewDiscographyGroupViewModel group, ArtistAlbumsResult results, IUIDispatcher _uidispatcher, int batchnumber)
        {
            using var mn = new ManualResetEvent(false);
            _uidispatcher.Invoke(() =>
            {
                var allItemsSinceStartOfBatch = group.Items
                    .SkipWhile(x => x.BatchNumber != batchnumber)
                    .ToArray();
                for (int i = 0; i < results.Albums.Count; i++)
                {
                    var fetchedAlbum = results.Albums.ElementAt(i);
                    var item = allItemsSinceStartOfBatch.ElementAt(i);
                    if (item.Value is not null)
                    {
                        continue;
                    }

                    var vm = BuildVm(fetchedAlbum);
                    item.Value = vm;

                    // foreach (var item in allItemsSinceStartOfBatch)
                    // {
                    //     item.Value = vm;
                    // }
                }
                mn.Set();
            });
            mn.WaitOne();
        }

        using (await _lock.LockAsync())
        {
            try
            {
                var group = Discography.FirstOrDefault(x => x.Type == arg2);
                if (group is null)
                {
                    return;
                }

                if (
                    _fetchedBatches.TryGetValue(arg2, out var batchForGroup)
                    &&
                    batchForGroup.TryGetValue(batchNumber, out var fetched))
                {
                    Set(group, fetched, _uiDispatcher, batchNumber);
                    return;
                }


                // Find minimum number in the batch 
                var min = batchNumber * batchSize;
                var results = await _mediator.Send(new GetAlbumsForArtistQuery()
                {
                    Id = _id,
                    Offset = min,
                    Limit = batchSize,
                    FetchTracks = false,
                    Group = group.Type
                });
                if (!_fetchedBatches.ContainsKey(arg2))
                {
                    _fetchedBatches.Add(arg2, new Dictionary<int, ArtistAlbumsResult>());
                }
                _fetchedBatches[arg2].Add(batchNumber, results);
                Set(group, results, _uiDispatcher, batchNumber);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public async Task FetchNextDiscography()
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

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    // public async Task FetchNextDiscography(bool initial)
    // {
    //     using (await _lock.LockAsync())
    //     {
    //         try
    //         {
    //             if (currentGroup is null)
    //             {
    //                 currentGroup = Discography.FirstOrDefault();
    //             }
    //
    //             if (currentGroup is null)
    //             {
    //                 return;
    //             }
    //
    //             var group = currentGroup;
    //             var totalItems = group.TotalItems;
    //             var alreadyAddedItems = group.Items.Count;
    //             var cachedItems = _discographyCache[group.Type]
    //                 .Skip(alreadyAddedItems)
    //                 .ToArray();
    //             var totalFutureItems = cachedItems.Length + alreadyAddedItems;
    //             //Check if we need to fetch more items from the API. 
    //             var needToFetch = totalFutureItems < totalItems;
    //             if (needToFetch && !initial)
    //             {
    //                 // Offset will be the total items before this group + the items we already have in this group.
    //
    //                 //Check if we actually need to fetch these
    //                 if (alreadyAddedItems < totalItems)
    //                 {
    //                     var results = await _mediator.Send(new GetAlbumsForArtistQuery()
    //                     {
    //                         Id = _id,
    //                         Offset = alreadyAddedItems + cachedItems.Length,
    //                         Limit = 30,
    //                         FetchTracks = false,
    //                         Group = group.Type
    //                     });
    //                     foreach (var album in results.Albums)
    //                     {
    //                         var vm = BuildVm(album);
    //                         _discographyCache[vm.Group].Add(vm);
    //                     }
    //                 }
    //
    //                 cachedItems = _discographyCache[group.Type]
    //                     .Skip(alreadyAddedItems)
    //                     .ToArray();
    //                 foreach (var item in cachedItems)
    //                 {
    //                     group.Items.Add(item);
    //                 }
    //
    //
    //                 totalFutureItems = group.Items.Count;
    //             }
    //             else
    //             {
    //                 // We have all the items we need, so we can just add them to the group.
    //                 foreach (var item in cachedItems)
    //                 {
    //                     group.Items.Add(item);
    //                 }
    //             }
    //
    //             //If we have all the items we need, we can move to the next group.
    //             if (totalFutureItems >= totalItems)
    //             {
    //                 var index = Discography.IndexOf(group);
    //                 if (index < Discography.Count - 1)
    //                 {
    //                     currentGroup = Discography[index + 1];
    //                 }
    //                 else
    //                 {
    //                     // Do nothing
    //                 }
    //             }
    //         }
    //         catch (Exception e)
    //         {
    //             Console.WriteLine(e);
    //         }
    //     }
    // }

    private static ArtistViewDiscographyItemViewModel BuildVm(SimpleAlbumEntity album)
    {
        var vm = new ArtistViewDiscographyItemViewModel
        {
            Group = album.Type?.ToLower() switch
            {
                "single" or "ep" => DiscographyGroupType.Single,
                "album" => DiscographyGroupType.Album,
                _ => DiscographyGroupType.Compilation
            },
            Album = new AlbumViewModel
            {
                Id = album.Id,
                Name = album.Name,
                MediumImageUrl = album.Images.OrderBy(x => x.Height).Skip(1).FirstOrDefault().Url ?? album.Images
                    .FirstOrDefault().Url,
                Year = album.Year ?? 0
            }
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


    private static string Format(ulong artistMonthlyListeners)
    {
        return artistMonthlyListeners.ToString("N0", CultureInfo.InvariantCulture);
    }
}