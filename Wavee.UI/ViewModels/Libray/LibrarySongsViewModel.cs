using System.Reactive.Concurrency;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using ReactiveUI;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Playback.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.Models.TrackSources;
using Wavee.UI.Playback.Contexts;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Libray
{
    public sealed partial class LibrarySongsViewModel : AbsLibraryViewModel<TrackViewModel>, ISortContext
    {
        private bool _initialized;
        private bool _sortAscending = true;
        private SortOption _sortBy;

        public SortOption? ExtraGroupString => SortBy switch
        {
            SortOption.Year => SortOption.Year,
            SortOption.Genre => SortOption.Genre,
            SortOption.Playcount => SortOption.Playcount,
            SortOption.LastPlayed => SortOption.LastPlayed,
            SortOption.DateAdded => SortOption.DateAdded,
            SortOption.BPM => SortOption.BPM,
            _ => SortOption.DateAdded
        };

        public SortOption SortBy
        {
            get => _sortBy;
            set
            {
                if (SetProperty(ref _sortBy, value))
                {
                    ShellViewModel.Instance.User.SavePreference("library.songs.sort", value);
                    OnPropertyChanged(nameof(ExtraGroupString));
                    SortChanged?.Invoke(this, (SortBy, SortAscending));
                    if (Tracks != null)
                    {
                        Tracks.SortBy = value;
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
                    if (Tracks != null)
                    {
                        Tracks.Ascending = value;
                    }
                }
            }
        }

        [ObservableProperty] private SortOption[]? _extendedSortOptions;

        public event EventHandler<(SortOption SortBy, bool SortAscending)>? SortChanged;

        public void DefaultSort()
        {
            SortBy = SortOption.DateAdded;
            SortAscending = false;
        }

        private readonly ILocalAudioDb _db;

        public LibrarySongsViewModel(ILocalAudioDb db)
        {
            _db = db;
            SortBy = ShellViewModel.Instance.User.ReadPreference<SortOption>("library.songs.sort");
            SortAscending = ShellViewModel.Instance.User.ReadPreference<bool>("library.songs.ascending");

            PlayCommand = new AsyncRelayCommand<TrackViewModel>(model =>
            {
                //Setup context
                IPlayContext? context = null;
                switch (Service)
                {
                    case ServiceType.Local:
                        //for local tracks, we just need to build a sql query:
                        //get the order of the tracks, the index of the track, and if we are filtering: the filters
                        //the filters could be: hearted, or (a search (TODO!))
                        var savedTracks = ShellViewModel.Instance.User.ForProfile.SavedTracks;
                        var filterQuery = (HeartedFilter
                            ? $"{nameof(LocalTrack.Id)} IN ({string.Join(",", savedTracks)})"
                            : "1=1");
                        context = new LocalFilesContext(SortBy, SortAscending, filterQuery);
                        break;
                    case ServiceType.Spotify:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (PlaybackViewModel.Instance.PlayingItem?.Equals(model.Track) is true)
                {
                    //pause or play?
                    return PlaybackViewModel.Instance.PauseResume();
                }

                if (context != null)
                {
                    return PlaybackViewModel.Instance!.Play(context, model.Index);
                }

                return Task.CompletedTask;
            });
        }


        public override Task Initialize()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }

            switch (Service)
            {
                case ServiceType.Local:
                    HasHeartedFilter = true;
                    //Artist, album, title, date added, duration are all part of the default sort

                    //we can also sort by
                    //year, genre, date modified, rating, play count, last played, bpm, 
                    ExtendedSortOptions = new[]
                    {
                        SortOption.Duration,
                        SortOption.Year,
                        SortOption.Genre,
                        SortOption.DateAdded,
                        SortOption.Playcount,
                        SortOption.LastPlayed,
                        SortOption.BPM
                    };
                    //fetch data from local service
                    Tracks = new SqlTracksSource(_db)
                    {
                        HeartedFilter = false,
                        Ascending = SortAscending,
                        SortBy = SortBy
                    };
                    break;
                case ServiceType.Spotify:
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }

        public override AsyncRelayCommand<TrackViewModel> PlayCommand
        {
            get;
        }

    }
}