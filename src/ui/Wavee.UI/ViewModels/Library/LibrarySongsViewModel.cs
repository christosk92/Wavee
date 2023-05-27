using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Remote.Messaging;
using Wavee.Spotify.Models.Response;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Services;

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
                           .ThenByDescending(x => x.Track.Album.Name.Length)
                           .ThenByAscending(x => x.Track.Album.ArtistName)
                           .ThenByAscending(x => x.Track.Album.DiscNumber)
                           .ThenByAscending(x => x.Track.TrackNumber),
                    LibraryTrackSortType.Added_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Added)
                            .ThenByDescending(x => x.Track.Album.Name.Length)
                            .ThenByAscending(x => x.Track.Album.ArtistName)
                            .ThenByAscending(x => x.Track.Album.DiscNumber)
                            .ThenByAscending(x => x.Track.TrackNumber),
                    LibraryTrackSortType.Title_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Title),
                    LibraryTrackSortType.Title_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Title),
                    LibraryTrackSortType.Artist_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Artists.First().Name)
                            .ThenByDescending(x => x.Track.Album.Name.Length)
                            .ThenByAscending(x => x.Track.Album.DiscNumber)
                            .ThenByAscending(x => x.Track.TrackNumber),
                    LibraryTrackSortType.Artist_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Artists.First().Name)
                        .ThenByDescending(x => x.Track.Album.Name.Length)
                        .ThenByAscending(x => x.Track.Album.DiscNumber)
                        .ThenByAscending(x => x.Track.TrackNumber),
                    LibraryTrackSortType.Album_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Album.Name)
                            .ThenByAscending(x => x.Track.Album.DiscNumber)
                            .ThenByAscending(x => x.Track.TrackNumber),
                    LibraryTrackSortType.Album_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Album.Name)
                            .ThenByDescending(x => x.Track.Album.DiscNumber)
                            .ThenByDescending(x => x.Track.TrackNumber),
                    _ => throw new ArgumentOutOfRangeException()
                })
                .ObserveOn(RxApp.TaskpoolScheduler);
        var country = Spotify<R>.CountryCode().Run(runtime).Result.ThrowIfFail().ValueUnsafe();
        var cdnUrl = Spotify<R>.CdnUrl().Run(runtime).ThrowIfFail().ValueUnsafe();
        var playCommand = ReactiveCommand.CreateFromTask<AudioId>(Execute);

        _cleanup = Library.Items
             .Filter(c => c.Id.Type is AudioItemType.Track)
             .TransformAsync(async item =>
             {
                 var response = await TrackEnqueueService<R>.GetTrack(item.Id);
                 var tr = SpotifyTrackResponse.From(country, cdnUrl,
                     response.Value.Match(Left: _ => throw new NotSupportedException(), Right: r => r));

                 return new LibraryTrack
                 {
                     Track = tr,
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
        var lookup = _data.AsEnumerable();
        switch (prm)
        {
            case LibraryTrackSortType.Added_Asc:
            case LibraryTrackSortType.Album_Asc:
            case LibraryTrackSortType.Artist_Asc:
            case LibraryTrackSortType.Duration_Asc:
            case LibraryTrackSortType.Title_Asc:
                lookup = BuildSort(lookup, prm, true);
                break;
            default:
                lookup = BuildSort(lookup, prm, false);
                break;
        }

        var index = lookup.Select(c => c.Track.Id).IndexOf(id);
        //pages are divided by 150 tracks
        //so if index = 150, page = 1
        var pageIndex = (index / 149) % (149);

        //spotify:user:7ucghdgquf6byqusqkliltwc2:collection
        var userId = ShellViewModel<R>.Instance.User.Id;
        var ctxId = $"spotify:user:{userId}:collection";
        var context = new PlayContextStruct(
            ContextId: ctxId,
            Index: index,
            TrackId: id,
            ContextUrl: $"context://{ctxId}",
            NextPages: None,
            PageIndex: pageIndex,
            Metadata: GetMetadataForSorting(prm)
        );

        await ShellViewModel<R>.Instance.Playback.PlayContextAsync(context);
    }

    private IEnumerable<LibraryTrack> BuildSort(IEnumerable<LibraryTrack> currentEnumerable, LibraryTrackSortType param, bool descending)
    {
        var prm = SortBy(param);
        var based = descending ?
            currentEnumerable.OrderByDescending(prm)
            : currentEnumerable.OrderBy(prm);

        switch (param)
        {
            case LibraryTrackSortType.Added_Desc:
            case LibraryTrackSortType.Added_Asc:
                //we need to sort by album, artist, disc, track
                return based
                    .ThenByDescending(x => x.Track.Album.Name.Length)
                    .ThenBy(x => x.Track.Album.ArtistName)
                    .ThenBy(x => x.Track.Album.DiscNumber)
                    .ThenBy(x => x.Track.TrackNumber);
                break;
            case LibraryTrackSortType.Title_Asc or LibraryTrackSortType.Title_Desc:
                return based;
            case LibraryTrackSortType.Artist_Asc or LibraryTrackSortType.Artist_Asc:
                return based
                    .ThenBy(x => x.Track.Album.Name.Length)
                    .ThenBy(x => x.Track.Album.DiscNumber)
                    .ThenBy(x => x.Track.TrackNumber);
                break;
            case LibraryTrackSortType.Album_Asc or LibraryTrackSortType.Album_Desc:
                return based
                    .ThenBy(x => x.Track.Album.ArtistName)
                    .ThenBy(x => x.Track.Album.DiscNumber)
                    .ThenBy(x => x.Track.TrackNumber);
                break;
            default:
                return based;
        }
    }

    private static Func<LibraryTrack, object> SortBy(LibraryTrackSortType prm)
    {
        return prm switch
        {
            LibraryTrackSortType.Added_Asc => x => x.Added,
            LibraryTrackSortType.Added_Desc => x => x.Added,
            LibraryTrackSortType.Title_Asc or LibraryTrackSortType.Title_Desc => x => x.Track.Title,
            LibraryTrackSortType.Artist_Asc or LibraryTrackSortType.Artist_Desc => x => x.Track.Artists[0].Name,
            LibraryTrackSortType.Album_Asc or LibraryTrackSortType.Album_Desc => x => x.Track.Album.Name.Length,
            LibraryTrackSortType.Duration_Asc or LibraryTrackSortType.Duration_Desc => x => x.Track.Duration,
            _ => throw new ArgumentOutOfRangeException(nameof(prm), prm, null)
        };
    }

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
        return t => t.Track.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || t.Track.Album.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
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
    public required ITrack Track { get; init; }
    public required DateTimeOffset Added { get; init; }

    public int OriginalIndex
    {
        get => _index;
        set => this.SetField(ref _index, value);
    }

    public required ICommand PlayCommand { get; init; }

    public string GetSmallestImage(ITrack track)
    {
        return track.Album.Artwork.OrderBy(i => i.Width).First().Url;
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
        return dateTimeOffset.ToString("g");
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