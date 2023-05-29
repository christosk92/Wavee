using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Models.Response;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.ViewModels;

public sealed class PlaylistViewModel<R> : ReactiveObject, INavigableViewModel
    where R : struct, HasSpotify<R>
{
    private readonly R runtime;
    private PlaylistViewData _playlist;
    private readonly SourceCache<PlaylistTrackInfo, string> _items = new(s => s.Uid);
    private IDisposable _listener;
    private IDisposable _cleanup;
    private readonly ReadOnlyObservableCollection<PlaylistTrackVm> _data;
    private string? _searchText;
    private PlaylistTrackSortType _sortParameters;

    private static ConcurrentDictionary<AudioId, TrackOrEpisode> _inmemoryCache
     = new();

    public PlaylistViewModel() { }

    public PlaylistViewModel(R runtime)
    {
        SortParameters = PlaylistTrackSortType.IndexAsc;
        this.runtime = runtime;
        SortCommand = ReactiveCommand.Create<PlaylistTrackSortType>(x =>
        {
            SortParameters = x;
        });

        var filterApplier =
            this.WhenValueChanged(t => t.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Select(propargs => BuildFilter(propargs))
                .ObserveOn(RxApp.TaskpoolScheduler);

        var sortChange =
            this.WhenValueChanged(t => t.SortParameters)
                .Select(c => c switch
                {
                    _ => SortExpressionComparer<PlaylistTrackVm>
                        .Ascending(x => x.OriginalIndex)
                })
                .ObserveOn(RxApp.TaskpoolScheduler);


        //TODO: Paging instead of loading all tracks at once
        _cleanup = _items
            .Connect()
            .Transform(x =>
            {
                //get track from cache
                // var track = Spotify<R>.GetFromCache(Seq1(x.Id)).Run(runtime).ThrowIfFail()[x.Id].ValueUnsafe();
                var track = _inmemoryCache[x.Id];
                var res = new PlaylistTrackVm
                {
                    Uid = x.Uid,
                    AddedAt = x.AddedAt,
                    Album = new PlaylistShortItem
                    {
                        Id = track.Group.Id,
                        Name = track.Group.Name,
                    },
                    Artists = track.Artists
                         .Select(f => new PlaylistShortItem
                         {
                             Id = f.Id,
                             Name = f.Name
                         }).ToArray(),
                    Duration = track.Duration,
                    HasAddedAt = x.AddedAt != DateTimeOffset.MinValue,
                    Id = x.Id,
                    Name = track.Name,
                    SmallestImage = track.GetImage(Image.Types.Size.Small),
                    OriginalIndex = _items.Items.IndexOf(x)
                };
                return res;
            })
            //.Filter(c => c.Data.Id.Type is AudioItemType.Track)
            .Filter(filterApplier)
            .Sort(sortChange)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)     // update observable collection bindings
            .DisposeMany()   //dispose when no longer required
            .Subscribe();
    }

    public string ImageUrl
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
    public PlaylistViewData Playlist
    {
        get => _playlist;
        set => this.RaiseAndSetIfChanged(ref _playlist, value);
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId playlistId)
            return;

        //playlists in spotify have a revision
        //check if we have the playlist in cache
        //we assume this is the latest revision
        //in the background, fetch the playlist again and do a diff
        //if the diff is empty, we're good
        //if the diff is not empty, we need to update the playlist which shoud be easy.

        //or we dont have the playlist in cache and we need to fetch it the first time
        //and then we need to keep it in cache

        Task.Run(async () =>
        {
            var imagesRes = await Spotify<R>
                .GetFromPublicApi<PotentialArtwork[]>($"/playlists/{playlistId.ToBase62()}/images",
                    CancellationToken.None)
                .Run(runtime);
            var images = imagesRes.ThrowIfFail();

            RxApp.MainThreadScheduler.Schedule(() => { ImageUrl = images.FirstOrDefault().Uri; });
        });

        var playlistMaybe = await Spotify<R>.GetPlaylistMaybeCached(playlistId)
            .Run(runtime);
        if (playlistMaybe.IsFail)
        {
            Debugger.Break();
            return;
        }

        var playlist = playlistMaybe.ThrowIfFail();

        Playlist = new PlaylistViewData
        {
            Name = playlist.Playlist.Attributes.Name,
            LargeImage = playlist.Playlist.Attributes.FormatAttributes
                .FirstOrDefault(c => c.Key is "header_image_url_desktop")?
                .Value,
            SmallImage = null,
            Description = playlist.Playlist.Attributes.Description,
            CanChangeDetails = playlist.Playlist.Capabilities
                .CanEditMetadata,
            OwnerUsername = playlist.Playlist.OwnerUsername,
            TotalTracks = (uint)playlist.Playlist.Contents.Items.Count
        };

        //setup a listener
        //when the playlist changes, we need to update the cache
        var firstDelta = new Diff
        {
            FromRevision = ByteString.Empty,
            ToRevision = playlist.Playlist.Revision,
            Ops = { new Op
            {
                Kind = Op.Types.Kind.Add,
                Add = new Add
                {
                    AddFirst = true,
                    Items = { playlist.Playlist.Contents.Items }
                }
            } }
        };
        //we need to somehow find a way to diff this revision based on the previous revision
        //https://spclient.wg.spotify.com/playlist/v2/playlist/3NmbmRX47JvWbQoa1rDS6Y/diff?revision=88%2Cff138db18d31270a0b07a2642f6d232cb9b9849e&handlesContent=
        //from this we get a list of OPS needed to update the playlist

        PlaylistFetched.TrySetResult();

        var baseObservable = Spotify<R>
            .ObservePlaylist(playlistId)
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe();
        var looseSubject = new Subject<Diff>();
        var mergedObservables = baseObservable.Merge(looseSubject);

        _listener = mergedObservables
            .StartWith(firstDelta)
            .SelectMany(async x =>
            {
                if (x.FromRevision == x.ToRevision)
                    return x;

                //fetch potential new tracks
                var potentialNewTracks = x.Ops
                    .Where(c => c.Kind is Op.Types.Kind.Add)
                    .SelectMany(c => c.Add.Items.Select(f => AudioId.FromUri(f.Uri)))
                    .ToSeq();

                //we do not care about the result here.
                var fetchedData = await TrackEnqueueService<R>.GetTracks(potentialNewTracks);
                foreach (var (key, value) in fetchedData)
                {
                    _inmemoryCache[key] = value.ValueUnsafe();
                }
                return x;
            })
            //.Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(diff =>
            {
                _items.Edit(innerList =>
                {
                    var oldList = innerList.Items.ToList();
                    innerList.Clear();
                    foreach (var op in diff.Ops)
                    {
                        switch (op.Kind)
                        {
                            case Op.Types.Kind.Add:
                                {
                                    //project the tracks to playlistinfo
                                    var newItems = op.Add.Items
                                        .Select(x => new PlaylistTrackInfo
                                        {
                                            AddedAt = x.Attributes.HasTimestamp
                                                ? DateTimeOffset.FromUnixTimeMilliseconds(x.Attributes.Timestamp)
                                                : DateTimeOffset.MinValue,
                                            HasAddedAt = x.Attributes.HasTimestamp,
                                            Id = AudioId.FromUri(x.Uri),
                                            Uid = x.Attributes.ItemId.ToBase64()
                                        });

                                    //add the items to the list (depending on index)
                                    if (op.Add.AddFirst)
                                    {
                                        //add to the front
                                        oldList.InsertRange(0, newItems);
                                    }
                                    else if (op.Add.AddLast)
                                    {
                                        oldList.AddRange(newItems);
                                    }
                                    else if (op.Add.HasFromIndex)
                                    {
                                        oldList.InsertRange(op.Add.FromIndex, newItems);
                                    }
                                    break;
                                }
                            case Op.Types.Kind.Rem:
                                if (op.Rem.HasFromIndex)
                                {
                                    oldList.RemoveRange(op.Rem.FromIndex, op.Rem.Length);
                                }
                                else if (op.Rem.HasItemsAsKey)
                                {
                                    //TODO:
                                }
                                break;
                            default:
                                Debugger.Break();
                                break;
                        }
                    }
                    innerList.Load(oldList);
                });
                GC.Collect();
            });
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public PlaylistTrackSortType SortParameters
    {
        get => _sortParameters;
        set => this.RaiseAndSetIfChanged(ref _sortParameters, value);
    }
    public ReadOnlyObservableCollection<PlaylistTrackVm> Data => _data;

    public bool IsSaved
    {
        get => _isSaved;
        set => this.RaiseAndSetIfChanged(ref _isSaved, value);
    }

    public ICommand SortCommand { get; }

    public TaskCompletionSource PlaylistFetched = new TaskCompletionSource();
    private bool _isSaved;
    private string _image;

    public void OnNavigatedFrom()
    {

    }
    private static Func<PlaylistTrackVm, bool> BuildFilter(string? searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return _ => true;
        return t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
    public void Clear()
    {
        _listener?.Dispose();
        _items?.Clear();
        _items?.Dispose();
        _cleanup?.Dispose();
    }
}

public readonly struct PotentialArtwork
{
    [JsonPropertyName("url")]
    public required string Uri { get; init; }
    [JsonPropertyName("width")]
    public required int? Width { get; init; }
    [JsonPropertyName("height")]
    public required int? Height { get; init; }
}

public enum PlaylistTrackSortType
{
    IndexAsc
}

public class PlaylistViewData
{
    public required string Name { get; init; } = null!;
    public required string? LargeImage { get; init; } = null!;
    public required string? SmallImage { get; init; } = null!;
    public required string? Description { get; init; } = null!;
    public required bool CanChangeDetails { get; init; }
    public required string OwnerUsername { get; init; }
    public required uint TotalTracks { get; init; }
}
public readonly record struct PlaylistTrackInfo(AudioId Id,
    string Uid,
    DateTimeOffset AddedAt, bool HasAddedAt);

public class PlaylistShortItem
{
    public string Name { get; set; }
    public AudioId Id { get; set; }
}
public class PlaylistTrackVm
{
    public int OriginalIndex { get; set; }
    public DateTimeOffset AddedAt { get; init; }
    public bool HasAddedAt { get; init; }

    public string SmallestImage
    {
        get;
        init;
    }
    public PlaylistShortItem Album { get; init; }
    public PlaylistShortItem[] Artists { get; init; }
    public AudioId Id { get; init; }
    public required string Uid { get; init; }
    public string Name { get; set; }
    public TimeSpan Duration { get; set; }

    public string FormatToRelativeDate(DateTimeOffset dateTimeOffset)
    {

        //less than 10 seconds: "Just now"
        //less than 1 minute: "X seconds ago"
        //less than 1 hour: "X minutes ago" OR // "1 minute ago"
        //less than 1 day: "X hours ago" OR // "1 hour ago"
        //less than 1 week: "X days ago" OR // "1 day ago"
        //Exact date

        var totalSeconds = (int)DateTimeOffset.Now.Subtract(dateTimeOffset).TotalSeconds;
        var totalMinutes = totalSeconds / 60;
        var totalHours = totalMinutes / 60;
        var totalDays = totalHours / 24;
        var totalWeeks = totalDays / 7;
        return dateTimeOffset switch
        {
            _ when dateTimeOffset > DateTimeOffset.Now.AddSeconds(-10) => "Just now",
            _ when dateTimeOffset > DateTimeOffset.Now.AddMinutes(-1) =>
                $"{totalSeconds} second{(totalSeconds > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddHours(-1) =>
                $"{totalMinutes} minute{(totalMinutes > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddDays(-1) =>
                $"{totalHours} hour{(totalHours > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddDays(-7) =>
                $"{totalDays} day{(totalDays > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddMonths(-1) =>
                $"{totalWeeks} week{(totalWeeks > 1 ? "s" : "")} ago",
            _ => GetFullMonthStr(dateTimeOffset)
        };

        static string GetFullMonthStr(DateTimeOffset d)
        {
            string fullMonthName =
                d.ToString("MMMM");
            return $"{fullMonthName} {d.Day}, {d.Year}";
        }
    }

    public string FormatToShorterTimestamp(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}