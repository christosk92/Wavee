using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Eum.Spotify.playlist4;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Playback;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibrarySongsViewModel<R> :
    ReactiveObject where R : struct, HasSpotify<R>, HasFile<R>, HasDirectory<R>, HasLocalPath<R>
{
    private readonly IDisposable _cleanup;
    private string? _searchText;
    private LibraryTrackSortType _sortParameters;
    private readonly ReadOnlyObservableCollection<LibraryTrack> _data;

    public LibrarySongsViewModel(R runtime)
    {
        SortParameters = LibraryTrackSortType.Added_Desc;
        SortCommand = ReactiveCommand.Create<LibraryTrackSortType>(x =>
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
                    LibraryTrackSortType.Added_Asc =>
                       SortExpressionComparer<LibraryTrack>.Ascending(x => x.Added)
                           .ThenByDescending(x => x.Track.AsTrack.Album.Name.Length)
                           .ThenByAscending(x => x.Track.AsTrack.Album.Artist[0].Name)
                           .ThenByAscending(x => x.Track.AsTrack.DiscNumber)
                           .ThenByAscending(x => x.Track.AsTrack.Number),
                    LibraryTrackSortType.Added_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Added)
                            .ThenByDescending(x => x.Track.AsTrack.Album.Name.Length)
                            .ThenByAscending(x => x.Track.AsTrack.Album.Artist[0].Name)
                            .ThenByAscending(x => x.Track.AsTrack.DiscNumber)
                            .ThenByAscending(x => x.Track.AsTrack.Number),
                    LibraryTrackSortType.Title_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Name),
                    LibraryTrackSortType.Title_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Name),
                    LibraryTrackSortType.Artist_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Artists.First().Name)
                            .ThenByDescending(x => x.Track.AsTrack.Album.Name.Length)
                            .ThenByAscending(x => x.Track.AsTrack.DiscNumber)
                            .ThenByAscending(x => x.Track.AsTrack.Number),
                    LibraryTrackSortType.Artist_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Artists.First().Name)
                        .ThenByDescending(x => x.Track.AsTrack.Album.Name.Length)
                        .ThenByAscending(x => x.Track.AsTrack.DiscNumber)
                        .ThenByAscending(x => x.Track.AsTrack.Number),
                    LibraryTrackSortType.Album_Asc =>
                        SortExpressionComparer<LibraryTrack>
                            .Ascending(x => x.Track.AsTrack.Album.Name)
                            .ThenByAscending(x => x.Track.AsTrack.DiscNumber)
                            .ThenByAscending(x => x.Track.AsTrack.Number),
                    LibraryTrackSortType.Album_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.AsTrack.Album.Name[0])
                            .ThenByDescending(x => x.Track.AsTrack.DiscNumber)
                            .ThenByDescending(x => x.Track.AsTrack.Number),
                    _ => throw new ArgumentOutOfRangeException()
                })
                .ObserveOn(RxApp.TaskpoolScheduler);
        var country = Spotify<R>.CountryCode().Run(runtime).Result.ThrowIfFail().ValueUnsafe();
        var playCommand = ReactiveCommand.CreateFromTask<AudioId>(Execute);


        var itemsTemp = new Dictionary<AudioId, Option<TrackOrEpisode>>();
        _cleanup = Library.Items
             .Filter(c => c.Id.Type is AudioItemType.Track)
             .SelectMany(async s =>
             {
                 var items = s.Select(f => f.Key).ToSeq();
                 itemsTemp = await TrackEnqueueService<R>.GetTracks(items);
                 return s;
             })
             .Transform(item =>
             {
                 return new LibraryTrack
                 {
                     Track = itemsTemp[item.Id].ValueUnsafe(),
                     Added = item.AddedAt,
                     PlayCommand = playCommand
                 };
             })
             .Filter(filterApplier)
             .Sort(sortChange)
             .ObserveOn(RxApp.MainThreadScheduler)
             .Bind(out _data)     // update observable collection bindings
             .DisposeMany()   //dispose when no longer required
             .Subscribe();
    }


    private async Task Execute(AudioId id)
    {
        var prm = SortParameters;
        var index = _data.Select(c => c.Track.Id)
            .IndexOf(id);
        //pages are divided by 150 tracks
        //so if index = 150, page = 1
        var pageIndex = (index / 149) % (149);

        //spotify:user:7ucghdgquf6byqusqkliltwc2:collection
        var userId = ShellViewModel<R>.Instance.User.Id;
        var ctxId = $"spotify:user:{userId}:collection";

        var sortMetadata = GetMetadataForSorting(prm);
        var txt = SearchText;
        if (!string.IsNullOrEmpty(txt))
        {
            //filtering.predicate
            //list_util_filter
            sortMetadata = sortMetadata.Add("filtering.predicate", $"text =^# \\\"{txt}\\\"");
            sortMetadata = sortMetadata.Add("list_util_filter", $"text contains {txt}");
        }
        var context = new PlayContextStruct(
            ContextId: ctxId,
            Index: index,
            TrackId: id,
            ContextUrl: $"context://{ctxId}",
            NextPages: None,
            PageIndex: pageIndex,
            Metadata: sortMetadata
        );

        await ShellViewModel<R>.Instance.Playback.PlayContextAsync(context);
    }

    // private IEnumerable<LibraryTrack> BuildSort(IEnumerable<LibraryTrack> currentEnumerable, LibraryTrackSortType param, bool descending)
    // {
    //     var prm = SortBy(param);
    //     var based = descending ?
    //         currentEnumerable.OrderByDescending(prm)
    //         : currentEnumerable.OrderBy(prm);
    //
    //     switch (param)
    //     {
    //         case LibraryTrackSortType.Added_Desc:
    //         case LibraryTrackSortType.Added_Asc:
    //             //we need to sort by album, artist, disc, track
    //             return based
    //                 .ThenByDescending(x => x.Track.Album.Name.Length)
    //                 .ThenBy(x => x.Track.Album.ArtistName)
    //                 .ThenBy(x => x.Track.Album.DiscNumber)
    //                 .ThenBy(x => x.Track.TrackNumber);
    //             break;
    //         case LibraryTrackSortType.Title_Asc or LibraryTrackSortType.Title_Desc:
    //             return based;
    //         case LibraryTrackSortType.Artist_Asc or LibraryTrackSortType.Artist_Asc:
    //             return based
    //                 .ThenBy(x => x.Track.Album.Name.Length)
    //                 .ThenBy(x => x.Track.Album.DiscNumber)
    //                 .ThenBy(x => x.Track.TrackNumber);
    //             break;
    //         case LibraryTrackSortType.Album_Asc or LibraryTrackSortType.Album_Desc:
    //             return based
    //                 .ThenBy(x => x.Track.Album.ArtistName)
    //                 .ThenBy(x => x.Track.Album.DiscNumber)
    //                 .ThenBy(x => x.Track.TrackNumber);
    //             break;
    //         default:
    //             return based;
    //     }
    // }

    // private static Func<LibraryTrack, object> SortBy(LibraryTrackSortType prm)
    // {
    //     return prm switch
    //     {
    //         LibraryTrackSortType.Added_Asc => x => x.Added,
    //         LibraryTrackSortType.Added_Desc => x => x.Added,
    //         LibraryTrackSortType.Title_Asc or LibraryTrackSortType.Title_Desc => x => x.Track.Title,
    //         LibraryTrackSortType.Artist_Asc or LibraryTrackSortType.Artist_Desc => x => x.Track.Artists[0].Name,
    //         LibraryTrackSortType.Album_Asc or LibraryTrackSortType.Album_Desc => x => x.Track.Album.Name.Length,
    //         LibraryTrackSortType.Duration_Asc or LibraryTrackSortType.Duration_Desc => x => x.Track.Duration,
    //         _ => throw new ArgumentOutOfRangeException(nameof(prm), prm, null)
    //     };
    // }

    private static HashMap<string, string> GetMetadataForSorting(LibraryTrackSortType sort) =>
        sort switch
        {
            LibraryTrackSortType.Added_Desc => new HashMap<string, string>()
               .Add("list_util_sort", "addTime DESC,album.name,album.artist.name,discNumber,trackNumber")
               .Add("sorting.criteria", "added_at DESC,album_title,album_artist_name,album_disc_number,album_track_number"),
            LibraryTrackSortType.Added_Asc => new HashMap<string, string>()
               .Add("list_util_sort", "addTime ASC,album.name,album.artist.name,discNumber,trackNumber")
               .Add("sorting.criteria", "added_at,album_title,album_artist_name,album_disc_number,album_track_number"),
            LibraryTrackSortType.Title_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "name ASC")
                .Add("sorting.criteria", "title"),
            LibraryTrackSortType.Title_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "name DESC")
                .Add("sorting.criteria", "title DESC"),
            LibraryTrackSortType.Artist_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "artist.name ASC,album.name,discNumber,trackNumber")
                .Add("sorting.criteria", "artist_name,album_title,album_disc_number,album_track_number"),
            LibraryTrackSortType.Artist_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "artist.name DESC,album.name,discNumber,trackNumber")
                .Add("sorting.criteria", "artist_name DESC,album_title,album_disc_number,album_track_number"),
            LibraryTrackSortType.Album_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name ASC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            LibraryTrackSortType.Album_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name DESC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            LibraryTrackSortType.Duration_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name ASC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            LibraryTrackSortType.Duration_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name ASC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, null)
        };
    public ReadOnlyObservableCollection<LibraryTrack> Data => _data;
    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public LibraryTrackSortType SortParameters
    {
        get => _sortParameters;
        set => this.RaiseAndSetIfChanged(ref _sortParameters, value);
    }
    public LibraryViewModel<R> Library => ShellViewModel<R>
        .Instance.Library;

    public ICommand SortCommand { get; }

    private static Func<LibraryTrack, bool> BuildFilter(string? searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return _ => true;
        return t => t.Track.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || t.Track.Group.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || t.Track.Artists.Any(a => a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }
    public void Dispose()
    {
        _cleanup.Dispose();
    }
}

public class LibraryTrack : INotifyPropertyChanged
{
    private int _index;
    public required TrackOrEpisode Track { get; init; }
    public required DateTimeOffset Added { get; init; }

    public int OriginalIndex
    {
        get => _index;
        set => this.SetField(ref _index, value);
    }

    public required ICommand PlayCommand { get; init; }

    public string GetSmallestImage(TrackOrEpisode track)
    {
        return track.GetImage(Image.Types.Size.Small);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

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
        //only show minutes and seconds
        return timeSpan.ToString(@"mm\:ss");
    }
}

public enum LibraryTrackSortType
{

    Title_Asc,
    Title_Desc,
    Artist_Asc,
    Artist_Desc,
    Album_Asc,
    Album_Desc,
    Duration_Asc,
    Duration_Desc,
    Added_Asc,
    Added_Desc
}