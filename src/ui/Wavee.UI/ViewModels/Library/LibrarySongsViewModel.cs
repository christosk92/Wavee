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
    private TrackSortType _sortParameters;
    private readonly ReadOnlyObservableCollection<LibraryTrack> _data;

    public LibrarySongsViewModel(R runtime)
    {
        SortParameters = TrackSortType.OriginalIndex_Asc;
        SortCommand = ReactiveCommand.Create<TrackSortType>(x =>
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
                    TrackSortType.OriginalIndex_Desc or TrackSortType.Added_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Added)
                            .ThenByDescending(x => x.Track.Album.Name.Length)
                            .ThenByAscending(x => x.Track.Album.ArtistName)
                            .ThenByAscending(x => x.Track.Album.DiscNumber)
                            .ThenByAscending(x => x.Track.TrackNumber),
                    TrackSortType.OriginalIndex_Asc or TrackSortType.Added_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Added)
                            .ThenByDescending(x => x.Track.Album.Name.Length)
                            .ThenByAscending(x => x.Track.Album.ArtistName)
                            .ThenByAscending(x => x.Track.Album.DiscNumber)
                            .ThenByAscending(x => x.Track.TrackNumber),
                    TrackSortType.Title_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Title),
                    TrackSortType.Title_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Title),
                    TrackSortType.Artist_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Artists.First().Name),
                    TrackSortType.Artist_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Artists.First().Name),
                    TrackSortType.Album_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Album.Name.Length),
                    TrackSortType.Album_Desc =>
                        SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Album.Name.Length),
                    TrackSortType.Duration_Asc =>
                        SortExpressionComparer<LibraryTrack>.Ascending(x => x.Track.Duration),
                    TrackSortType.Duration_Desc =>
                            SortExpressionComparer<LibraryTrack>.Descending(x => x.Track.Duration),
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
            case TrackSortType.Added_Asc:
            case TrackSortType.Album_Asc:
            case TrackSortType.Artist_Asc:
            case TrackSortType.Duration_Asc:
            case TrackSortType.OriginalIndex_Desc:
            case TrackSortType.Title_Asc:
                lookup = BuildSort(lookup, prm, true);
                //lookup = lookup.OrderBy(SortBy(prm));
                break;
            case TrackSortType.OriginalIndex_Asc:
            default:
                lookup = BuildSort(lookup, prm, false);
                // lookup = lookup.OrderByDescending(SortBy(prm));
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

    private IEnumerable<LibraryTrack> BuildSort(IEnumerable<LibraryTrack> currentEnumerable, TrackSortType param, bool descending)
    {
        var prm = SortBy(param);
        var based = descending ?
            currentEnumerable.OrderByDescending(prm)
            : currentEnumerable.OrderBy(prm);

        switch (param)
        {
            case TrackSortType.OriginalIndex_Asc:
            case TrackSortType.Added_Desc:
            case TrackSortType.OriginalIndex_Desc:
            case TrackSortType.Added_Asc:
                //we need to sort by album, artist, disc, track
                return based
                    .ThenBy(x => x.Track.Album.Name.Length)
                    .ThenBy(x => x.Track.Artists[0].Name)
                    .ThenBy(x => x.Track.Album.DiscNumber)
                    .ThenBy(x => x.Track.TrackNumber);
                break;
            default:
                //TODO:
                return based;
                break;
        }
    }

    private static Func<LibraryTrack, object> SortBy(TrackSortType prm)
    {
        return prm switch
        {
            TrackSortType.OriginalIndex_Desc or TrackSortType.Added_Asc => x => x.Added,
            TrackSortType.OriginalIndex_Asc or TrackSortType.Added_Desc => x => x.Added,
            TrackSortType.Title_Asc or TrackSortType.Title_Desc => x => x.Track.Title,
            TrackSortType.Artist_Asc or TrackSortType.Artist_Desc => x => x.Track.Artists[0].Name,
            TrackSortType.Album_Asc or TrackSortType.Album_Desc => x => x.Track.Album.Name.Length,
            TrackSortType.Duration_Asc or TrackSortType.Duration_Desc => x => x.Track.Duration,
            _ => throw new ArgumentOutOfRangeException(nameof(prm), prm, null)
        };
    }

    private static HashMap<string, string> GetMetadataForSorting(TrackSortType sort) =>
        sort switch
        {
            TrackSortType.OriginalIndex_Asc or TrackSortType.Added_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "addTime DESC,album.name,album.artist.name,discNumber,trackNumber")
                .Add("sorting.criteria", "added_at DESC,album_title,album_artist_name,album_disc_number,album_track_number"),
            TrackSortType.OriginalIndex_Desc or TrackSortType.Added_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "addTime ASC,album.name,album.artist.name,discNumber,trackNumber")
                .Add("sorting.criteria", "added_at,album_title,album_artist_name,album_disc_number,album_track_number"),
            TrackSortType.Title_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "name ASC")
                .Add("sorting.criteria", "title"),
            TrackSortType.Title_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "name DESC")
                .Add("sorting.criteria", "title DESC"),
            TrackSortType.Artist_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "artist.name ASC,album.name,discNumber,trackNumber")
                .Add("sorting.criteria", "artist_name,album_title,album_disc_number,album_track_number"),
            TrackSortType.Artist_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "artist.name DESC,album.name,discNumber,trackNumber")
                .Add("sorting.criteria", "artist_name DESC,album_title,album_disc_number,album_track_number"),
            TrackSortType.Album_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "artist.name DESC,album.name,discNumber,trackNumber")
                .Add("sorting.criteria", "artist_name DESC,album_title,album_disc_number,album_track_number"),
            TrackSortType.Album_Desc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name ASC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            TrackSortType.Duration_Asc => new HashMap<string, string>()
                .Add("list_util_sort", "album.name ASC,discNumber,trackNumber")
                .Add("sorting.criteria", "album_title,album_disc_number,album_track_number"),
            TrackSortType.Duration_Desc => new HashMap<string, string>()
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

    public TrackSortType SortParameters
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

public enum TrackSortType
{
    OriginalIndex_Asc,
    OriginalIndex_Desc,
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