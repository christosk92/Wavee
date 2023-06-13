using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Eum.Spotify.extendedmetadata;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Infrastructure.IO;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.ApResolve;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.UI.Client;
using static LanguageExt.Prelude;
namespace Wavee.UI.ViewModels.Playlists.Specific;

public sealed class PlaylistViewModel : ObservableObject
{
    //    private readonly SourceCache<PlaylistTrackViewModel, ByteString> _sourceCache = new(x => x.Uid);
    private string _name;
    private string? _image;
    private readonly CompositeDisposable _listeners;
    private bool _isBusy;
    private string _searchText;
    private PlaylistSortParameter _sort;
    private int _currentPage = 1;

    private const int PageSize = 100;

    private PlaylistSortParameter _lastSortOrder;
    public PlaylistViewModel(Action<Action> invokeOnUiThread)
    {
        var fetchParameters = this.WhenAnyValue(
            x => x.Sort,
            x => x.SearchText,
            x => x.CurrentPage,
            (sortColumn, filterString, page) => new { sortColumn, filterString, page });

        var dataFetcher = fetchParameters
            .Skip(1) //skip initial value
                     //.Throttle(TimeSpan.FromMilliseconds(250)) // Optional: rate limit the fetching
            .ObserveOn(RxApp.TaskpoolScheduler)
            .SelectMany(async x =>
            {  // Fetch data from the database using the sort and filter parameters
                // Check if sort order has changed
                if (_lastSortOrder != x.sortColumn)
                {
                    // Clear cache and reset current page to 1
                    invokeOnUiThread(() =>
                    {
                        Tracks.Clear();
                        CurrentPage = 1;
                        _lastSortOrder = x.sortColumn;
                    });
                }

                // Fetch data from the database using the sort, filter, and paging parameters
                var fetchedData = await FetchDataFromDatabase(x.sortColumn, x.filterString, x.page);
                return fetchedData;
            })
            .Subscribe(x =>
            {
                invokeOnUiThread(() =>
                {
                    // Add the fetched data to the cache
                    Tracks.AddRange(x);
                });
            });


        _listeners = new CompositeDisposable(dataFetcher);
    }
    public ObservableCollection<PlaylistTrackViewModel> Tracks { get; } = new();
    public AudioId Id { get; private set; }
    public SelectedListContent SelectedListContent { get; private set; }

    public PlaylistSortParameter Sort
    {
        get => _sort;
        set => this.SetProperty(ref _sort, value);
    }
    public int CurrentPage
    {
        get => _currentPage;
        set => this.SetProperty(ref _currentPage, value);
    }

    public string Name
    {
        get => _name;
        set => this.SetProperty(ref _name, value);
    }
    public string SearchText
    {
        get => _searchText;
        set => this.SetProperty(ref _searchText, value);
    }
    public string? Image
    {
        get => _image;
        set => this.SetProperty(ref _image, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.SetProperty(ref _isBusy, value);
    }

    public async Task Create(AudioId id, CancellationToken ct = default)
    {
        IsBusy = true;
        Id = id;
        var playlistMaybe = await Task.Run(async () => await SpotifyView.FetchPlaylist(id, ct).Run(), ct);
        if (playlistMaybe.IsFail)
        {
            //show error
            return;
        }

        var (playlist, fromCache) = playlistMaybe.Match(Succ: x => x, Fail: _ => throw new NotSupportedException("This should never happen."));
        SelectedListContent = playlist;

        var ids = playlist.Contents.Items
            .ToDictionary(c => AudioId.FromUri(c.Uri));

        Name = playlist.Attributes.Name;
        await Task.Run(async () => await FetchMissingTracks(ids.Keys.ToSeq(), ct), ct);
        var fetchedData = await FetchDataFromDatabase(Sort, null, 1);
        Tracks.AddRange(fetchedData);
        if (fromCache)
        {
            //do a diff;
        }


        IsBusy = false;
    }
    public async Task Create(PlaylistSidebarItem sidebarItem)
    {
        Name = sidebarItem.Name;
        Id = AudioId.FromUri(sidebarItem.Id);
    }
    public void Destroy()
    {
        _listeners?.Dispose();
        //_sourceCache.Dispose();
    }

    private async Task<IEnumerable<PlaylistTrackViewModel>> FetchDataFromDatabase(PlaylistSortParameter sortColumn, string filterString, int page)
    {
        // Fetch data from the database using the sort, filter, and paging parameters
        //var fetchedData = await _database.FetchData(sortColumn, filterString, page, PageSize);
        //return fetchedData;
        if (SelectedListContent is null)
            return Enumerable.Empty<PlaylistTrackViewModel>();
        var skip = (page - 1) * PageSize;
        var take = PageSize;

        var client = State.Instance.Client;
        var asDictionary = SelectedListContent.Contents.Items
            .ToDictionary(c => AudioId.FromUri(c.Uri));

        var indices = SelectedListContent.Contents.Items.Select((c, i) => (c, i))
            .ToDictionary(c => c.i, c => c.c);

        var ids = asDictionary.Keys.ToSeq();
        switch (sortColumn)
        {
            case PlaylistSortParameter.OriginalIndex_Asc:
                var fetchedData = await client.Cache.GetTracksOriginalSort(ids.Skip(skip).Take(take), filterString);
                var resultAsDictionary = fetchedData.ToDictionary(x => x.Id, x => x);

                var result = new List<PlaylistTrackViewModel>();
                foreach (var item in indices.Skip(skip).Take(take))
                {
                    var id = AudioId.FromUri(item.Value.Uri);
                    var track = resultAsDictionary[id];
                    result.Add(new PlaylistTrackViewModel
                    {
                        Id = id,
                        OriginalIndex = item.Key,
                        Uid = item.Value.Attributes.ItemId,
                        View = track
                    });
                }

                return result;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sortColumn), sortColumn, null);
        }
    }

    private async Task FetchMissingTracks(Seq<AudioId> items, CancellationToken ct = default)
    {
        var client = State.Instance.Client;


        var sw = Stopwatch.StartNew();
        var cachedTracks = client.Cache.CheckExists(items);
        sw.Stop();
        var uncachedTracks = items.Where((_, i) => !cachedTracks[i])
            .ToArray();
        if (uncachedTracks.Length == 0)
            return;

        var batchesOf2000 = uncachedTracks.Chunk(2000);


        var spClient = ApResolver.SpClient.ValueUnsafe();
        var url = $"https://{spClient}/extended-metadata/v0/extended-metadata?market=from_token";

        static Aff<Unit> GetBatch(AudioId[] audioIds, SpotifyClient client, string url,
            CancellationToken ct)
        {
            return
                from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
                    .Map(x => new AuthenticationHeaderValue("Bearer", x))
                from content in Eff(() =>
                {
                    var request = new BatchedEntityRequest();
                    request.EntityRequest.AddRange(audioIds.Select(a => new EntityRequest
                    {
                        EntityUri = a.ToString(),
                        Query =
                        {
                            new ExtensionQuery
                            {
                                ExtensionKind = a.Type switch
                                {
                                    AudioItemType.Track => ExtensionKind.TrackV4,
                                    AudioItemType.PodcastEpisode => ExtensionKind.EpisodeV4,
                                    _ => ExtensionKind.UnknownExtension
                                }
                            }
                        }
                    }));
                    request.Header = new BatchedEntityRequestHeader
                    {
                        Catalogue = "premium",
                        Country = client.CountryCode.ValueUnsafe()
                    };
                    var byteArrCnt = new ByteArrayContent(request.ToByteArray());
                    //byteArrCnt.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.collection-v2.spotify.proto");
                    return byteArrCnt;
                })
                from posted in HttpIO.Post(url, bearer,
                        LanguageExt.HashMap<string, string>.Empty,
                        content, ct)
                    .ToAff()
                    .MapAsync(async x =>
                    {
                        x.EnsureSuccessStatusCode();
                        await using var stream = await x.Content.ReadAsStreamAsync(ct);
                        var response = BatchedExtensionResponse.Parser.ParseFrom(stream);
                        var allData = response
                            .ExtendedMetadata
                            .SelectMany(c =>
                            {
                                return c.ExtensionKind switch
                                {
                                    ExtensionKind.EpisodeV4 => c.ExtensionData
                                        .Select(e => new TrackOrEpisode(
                                            Either<Episode, Lazy<Track>>.Left(
                                                Episode.Parser.ParseFrom(e.ExtensionData.Value))
                                        )),
                                    ExtensionKind.TrackV4 => c.ExtensionData
                                        .Select(e => new TrackOrEpisode(
                                            Either<Episode, Lazy<Track>>.Right(new Lazy<Track>(() =>
                                                Track.Parser.ParseFrom(e.ExtensionData.Value)))
                                        )),
                                };
                            });

                        return allData.ToSeq();
                    })
                from _ in Eff(() => client.Cache.SaveBulk(posted))
                select unit;
        }

        foreach (var batch in batchesOf2000)
        {
            var batched = await GetBatch(batch, client, url, ct).Run();
            _ = batched.Match(Succ: x => unit,
                Fail: x => throw x);
        }
    }

    public void NextPage()
    {
        CurrentPage++;
    }
}

public enum PlaylistSortParameter
{
    OriginalIndex_Asc
}