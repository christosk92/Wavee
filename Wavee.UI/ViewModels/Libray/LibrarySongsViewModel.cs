using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.Models.Local;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Libray
{
    public sealed partial class LibrarySongsViewModel : AbsLibraryViewModel, ISortContext
    {
        private bool _initialized;
        private bool _sortAscending = true;
        private string? _sortBy;

        [ObservableProperty] private string? _extraGroupString;

        [ObservableProperty] private IEnumerable<LibraryTrackViewModel>? _tracks;

        public string? SortBy
        {
            get => _sortBy;
            set
            {
                if (value != null)
                {
                    if (SetProperty(ref _sortBy, value))
                    {
                        ShellViewModel.Instance.User.SavePreference("library.songs.sort", value);
                        SortChanged?.Invoke(this, (SortBy, SortAscending));
                        Initialize(true);
                    }
                }
            }
        }

        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                {
                    ShellViewModel.Instance.User.SavePreference("library.songs.ascending", value);
                    SortChanged?.Invoke(this, (SortBy, SortAscending));
                    Initialize(true);
                }
            }
        }

        public event EventHandler<(string SortBy, bool SortAscending)>? SortChanged;

        public void DefaultSort()
        {
            SortBy = "Date Imported";
            SortAscending = false;
        }

        [ObservableProperty] private string[]? _extendedSortOptions;

        private readonly ILocalAudioDb _db;
        private readonly IPlaycountService _playcountService;

        public LibrarySongsViewModel(ILocalAudioDb db, IPlaycountService playcountService)
        {
            _db = db;
            _playcountService = playcountService;
            SortBy = ShellViewModel.Instance.User.ReadPreference<string>("library.songs.sort");
            SortAscending = ShellViewModel.Instance.User.ReadPreference<bool>("library.songs.ascending");
        }


        public override Task Initialize(bool ignoreAlreadyInitialized = false)
        {
            if (!ignoreAlreadyInitialized && _initialized)
            {
                //fetch data from local service
                return Task.CompletedTask;
            }

            switch (Service)
            {
                case ServiceType.Local:
                    HasHeartedFilter = true;
                    //fetch data from local service

                    //Default sort =  Date Added Descending
                    if (!ignoreAlreadyInitialized && string.IsNullOrEmpty(SortBy))
                    {
                        _sortBy = "Date Imported";
                        _sortAscending = false;
                        _initialized = true;
                    }

                    return HandleLocalServiceTracks();
                case ServiceType.Spotify:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }

        private async Task HandleLocalServiceTracks(CancellationToken ct = default)
        {
            try
            {
                //Artist, album, title, date added, duration are all part of the default sort

                //we can also sort by
                //year, genre, date modified, rating, play count, last played, bpm, 
                ExtendedSortOptions = new[]
                {
                    "Duration",
                    "Year",
                    "Genre",
                    "Date Modified",
                    "Playcount",
                    "Last Played",
                    "BPM"
                };


                var query = BuildSqlQuery(SortBy, SortAscending, HeartedFilter);
                var tracks = (await _db.ReadTracks(query, true, ct)).ToArray();

                ExtraGroupString = SortBy switch
                {
                    "Year" => "Year",
                    "Genre" => "Genres",
                    "Date Modified" => "Date Modified",
                    "Playcount" => "Playcount",
                    "Last Played" => "Last Played",
                    "BPM" => "BPM",
                    _ => "Date Imported"
                };

                Tracks = tracks.Select((t, i) =>
                {
                    //get the extra string data depending on the sort option
                    var extraStringData = SortBy switch
                    {
                        "Year" => t.Year.ToString(),
                        "Genre" => string.Join(",", t.Genres),
                        "Date Modified" => t.LastChanged.ToString("o"),
                        "Playcount" => t.Playcount.ToString(),
                        "Last Played" => t.LastPlayed.ToString("o"),
                        "BPM" => t.BeatsPerMinute.ToString(),
                        _ => t.DateImported.ToString("o")
                    };

                    return new LibraryTrackViewModel(t, i, extraStringData, null);
                });
            }
            catch (Exception x)
            {
            }
        }

        private static string BuildSqlQuery(string sortBy, bool ascending, bool onlyHearted)
        {
            //the db handles the SELECT FROM statement
            //we just need to build the ORDER BY statement
            //if onlyHearted = true, then we need to add a WHERE statement based on the users hearted tracks

            //if we are sorting by playcount or last played, we need to get the playcount and last played data from the playcount service
            // so we need to lookup the playcount table based on the track id
            //we can then use the playcount and last played data in the ORDER BY statement

            var savedTracks = ShellViewModel.Instance.User.ForProfile.SavedTracks;
            const string where = "WHERE ";
            var filterQuery = where +
                              (onlyHearted ? $"{nameof(LocalTrack.Id)} IN ({string.Join(",", savedTracks)})" : "1=1");

            var orderQueryAppend = ascending ? "ASC" : "DESC";



            const string min = "0001-01-01";

            const string baseQuery =
                $@"SELECT mi.*, COALESCE(pc.Playcount, 0) AS Playcount, COALESCE(pc.LastPlayed, '{min}') AS LastPlayed FROM MediaItems mi LEFT JOIN (SELECT TrackId, COUNT(*) AS Playcount, MAX(DatePlayed) AS LastPlayed FROM Playcount GROUP BY TrackId) pc ON mi.Id = pc.TrackId";


            const string sql = "ORDER BY ";
            var orderQuery = sql + sortBy switch
            {
                "Year" => nameof(LocalTrack.Year),
                "Genre" => nameof(LocalTrack.Genres),
                "Date Modified" => nameof(LocalTrack.LastChanged),
                "Playcount" => "COALESCE(pc.Playcount, 0)",
                "Last Played" => $"COALESCE(pc.LastPlayed, {min})",
                "BPM" => nameof(LocalTrack.BeatsPerMinute),
                "Title" => nameof(LocalTrack.Title),
                "Artist" => nameof(LocalTrack.Performers),
                "Album" => nameof(LocalTrack.Album),
                "Duration" => nameof(LocalTrack.Duration),
                _ => nameof(LocalTrack.DateImported)
            };

            var total = $"{baseQuery} {filterQuery} {orderQuery} {orderQueryAppend}";
            return total;
        }
    }
}