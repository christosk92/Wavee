using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
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
    private readonly SourceCache<PlaylistTrackVm, AudioId> _items = new(s => s.Id);
    private IDisposable _listener;
    private IDisposable _cleanup;
    private ReadOnlyObservableCollection<PlaylistTrackVm> _data;
    private string? _searchText;
    private PlaylistTrackSortType _sortParameters;

    public PlaylistViewModel()
    {

    }
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


        _cleanup = _items
            .Connect()
            //.Filter(c => c.Data.Id.Type is AudioItemType.Track)
            .Filter(filterApplier)
            .Sort(sortChange)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _data)     // update observable collection bindings
            .DisposeMany()   //dispose when no longer required
            .Subscribe();
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
            ToRevision = ByteString.Empty
        };

        PlaylistFetched.TrySetResult();

        _listener = Spotify<R>
            .ObservePlaylist(playlistId)
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .StartWith(firstDelta)
            .SelectMany(async x =>
            {
                if (ReferenceEquals(x, firstDelta))
                {
                    var tracksToQueue = playlist.Playlist
                        .Contents.Items.Select(c => AudioId.FromUri(c.Uri)).ToSeq();
                    if (tracksToQueue.IsEmpty)
                    {
                        return Enumerable.Empty<PlaylistTrackVm>();
                    }

                    var tracks = await TrackEnqueueService<R>.GetTracks(tracksToQueue);
                    var toReturn = new List<PlaylistTrackVm>(playlist.Playlist
                        .Contents.Items.Count);
                    Parallel.ForEach(playlist.Playlist
                        .Contents.Items.Select((y, i) => (y, i)), new ParallelOptions
                        {
                            MaxDegreeOfParallelism = playlist.Playlist
                                .Contents.Items.Count
                        }, (d) =>
                        {
                            var (request, index) = d;
                            var id = AudioId.FromUri(request.Uri);
                            if (tracks.TryGetValue(id, out var track) && track.IsSome)
                            {
                                var trcKpr = new TrackOrEpisode(track.ValueUnsafe().Value);
                                var item = new PlaylistTrackVm
                                {
                                    AddedAt = request.Attributes.HasTimestamp
                                        ? DateTimeOffset.FromUnixTimeMilliseconds(request.Attributes.Timestamp)
                                        : DateTimeOffset.MinValue,
                                    HasAddedAt = request.Attributes.HasTimestamp,
                                    OriginalIndex = index,
                                    Id = id,
                                    Album = new PlaylistShortItem
                                    {
                                        Id = trcKpr.Group.Id,
                                        Name = trcKpr.Group.Name
                                    },
                                    Artists = trcKpr.Artists.Select(c => new PlaylistShortItem
                                    {
                                        Id = c.Id,
                                        Name = c.Name
                                    }).ToArray(),
                                    Name = trcKpr.Name,
                                    SmallestImage = trcKpr.GetImage(Image.Types.Size.Small),
                                    Duration = trcKpr.Duration
                                };
                                toReturn.Add(item);
                            }
                            else
                            {
                                Debugger.Break();
                            }
                        });

                    return toReturn;
                    //return results.Select(c=> new PlaylistTrackInfo(c, ));
                }
                else
                {
                    return Enumerable.Empty<PlaylistTrackVm>();
                }
            })
            //.Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(tracks =>
            {
                GC.Collect();
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    foreach (var track in tracks)
                    {
                        innerList.AddOrUpdate(track);
                    }
                });
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
public readonly record struct PlaylistTrackInfo(
    PlaylistTrackVm Data, DateTimeOffset AddedAt, bool HasAddedAt, int Index);

public class PlaylistShortItem
{
    public string Name { get; set; }
    public AudioId Id { get; set; }
}
public class PlaylistTrackVm
{
    public int OriginalIndex { get; init; }
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