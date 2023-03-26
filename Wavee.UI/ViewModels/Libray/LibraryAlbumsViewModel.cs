using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.Models;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models;
using Wavee.UI.Models.Local;
using Wavee.UI.Models.TrackSources;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Libray
{
    public partial class LibraryAlbumsViewModel : AbsLibraryViewModel<object>
    {
        private bool _initialized;

        [ObservableProperty] private SortOption _sortBy;
        [ObservableProperty] private bool _sortAscending;
        [ObservableProperty] private object? _albumsSource;
        [ObservableProperty] private bool _isGrouped;
        private readonly ILocalAudioDb _db;

        public LibraryAlbumsViewModel(ILocalAudioDb db)
        {
            _db = db;
            SortBy = SortOption.None;
            SortAscending = false;
        }

        public async override Task Initialize()
        {
            if (_initialized)
            {
                return;
            }

            switch (Service)
            {
                case ServiceType.Local:
                    HasHeartedFilter = false;
                    //Artist, album, title, date added, duration are all part of the default sort

                    //we can also sort by
                    //year, genre, date modified, rating, play count, last played, bpm, 
                    // ExtendedSortOptions = new[]
                    // {
                    //     SortOption.Duration,
                    //     SortOption.Year,
                    //     SortOption.Genre,
                    //     SortOption.DateAdded,
                    //     SortOption.Playcount,
                    //     SortOption.LastPlayed,
                    //     SortOption.BPM
                    // };
                    //fetch data from local service
                    var src = (await GetSqlAlbums())
                        .GroupBy(a => a.Album.Year)
                        .Select(a => new GroupedSource(a)
                        {
                            Key = a.Key.ToString()
                        });
                    if (SortBy is SortOption.None)
                    {
                        IsGrouped = false;
                        AlbumsSource = src.SelectMany(a => a);
                    }
                    else
                    {
                        IsGrouped = true;
                        AlbumsSource = src;
                    }
                    break;
                case ServiceType.Spotify:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override AsyncRelayCommand<TrackViewModel> PlayCommand
        {
            get;
        }

        private async Task<IEnumerable<AlbumViewModel>> GetSqlAlbums(CancellationToken ct = default)
        {
            try
            {
                var savedAlbums = ShellViewModel.Instance.User.ForProfile.SavedAlbums;
                var albums = await _db.GetAlbums(_sortBy,
                    _sortAscending,
                    savedAlbums,
                    ct);
                // var query = BuildSqlQuery(SortOption.Year, false, HeartedFilter, 0, 10);
                //var tracks = (await _db.ReadTracks(query, true, ct)).ToArray();
                //any other sort option will use the default sort option as the group string

                // return tracks
                //     .Select(a => new AlbumViewModel(new LocalAlbum(
                //         Image: a.Image,
                //         Title: a.Album,
                //         Service: ServiceType.Local,
                //         Artists: a.Performers.Select(performer => new DescriptionItem(performer, null, null)).ToImmutableArray(),
                //         Year: a.Year
                //     )));
                return albums
                    .Select(a => new AlbumViewModel(a));
            }
            catch (Exception x)
            {
                return Enumerable.Empty<AlbumViewModel>();
            }
        }
    }
}