using NeoSmart.AsyncLock;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Mediator;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Test;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Track;

namespace Wavee.UI.Features.Library.ViewModels;

public sealed class LibrarySongsViewModel : NavigationItemViewModel
{
    private bool _tracksLoaded;
    private string _generalSearchTerm = string.Empty;
    private readonly AsyncLock _lock = new AsyncLock();
    private TimeSpan? _totalDuration;

    private readonly IMediator _mediator;
    private readonly IUIDispatcher _uiDispatcher;
    private string _sortField = nameof(LibraryItem<SimpleTrackEntity>.AddedAt);
    private ObservableCollection<LazyPlaylistTrackViewModel> _tracks;

    public LibrarySongsViewModel(IMediator mediator, IUIDispatcher uiDispatcher)
    {
        _mediator = mediator;
        _uiDispatcher = uiDispatcher;
        Tracks = new ObservableCollection<LazyPlaylistTrackViewModel>();
    }

    public string SortField
    {
        get => _sortField;
        set => SetProperty(ref _sortField, value);
    }

    public bool TracksLoaded
    {
        get => _tracksLoaded;
        set => SetProperty(ref _tracksLoaded, value);
    }

    public ObservableCollection<LazyPlaylistTrackViewModel> Tracks
    {
        get => _tracks;
        set => SetProperty(ref _tracks, value);
    }

    public TimeSpan? TotalDuration
    {
        get => _totalDuration;
        set => SetProperty(ref _totalDuration, value);
    }
    public string GeneralSearchTerm
    {
        get => _generalSearchTerm;
        set => SetProperty(ref _generalSearchTerm, value);
    }

    public ObservableCollection<string> SearchTerms { get; } = new();

    public async Task RefreshTracks()
    {
        await FetchAndSetTracks();
    }

    private async Task FetchAndSetTracks()
    {
        using (await _lock.LockAsync())
        {
            var searchTerms = SearchTerms.ToList();
            if (!string.IsNullOrWhiteSpace(GeneralSearchTerm))
            {
                searchTerms.Add(GeneralSearchTerm);
            }

            var trackIdsAndAttributes = await _mediator.Send(new GetLibrarySongsQuery
            {
                Search = searchTerms,
                SortField = TrackLibrarySortField.Added,
                SortDescending = true
            });

            _uiDispatcher.Invoke(() =>
            {
                try
                {
                    Tracks?.Clear();
                }
                catch (COMException e)
                {
                    Tracks = new ObservableCollection<LazyPlaylistTrackViewModel>();
                }

                int index = 0;
                TracksLoaded = true;
                double totalSeconds = 0;
                var tempTracks = new List<LazyPlaylistTrackViewModel>();
                foreach (var root in trackIdsAndAttributes.Items)
                {
                    var track = root.Item;
                    PlaylistTrackViewModel? trackasVm = null;
                    trackasVm = new PlaylistTrackViewModel(
                        spotifyTrack: track, 
                        addedAt: root.AddedAt);
                    totalSeconds += trackasVm.Duration.TotalSeconds;

                    tempTracks.Add(new LazyPlaylistTrackViewModel
                    {
                        HasValue = true,
                        Track = trackasVm!,
                        Index = index++
                    });
                }

                index = 0;
                foreach (var trackToAdd in tempTracks.OrderByDescending(f => f.Track!.AddedAt))
                {
                    trackToAdd.Index = index++;
                    Tracks.Add(trackToAdd);
                }

                TotalDuration = TimeSpan.FromSeconds(totalSeconds);
            });
        }
    }

    public async Task OnLibraryChanged_Dumb()
    {
        await RefreshTracks();
    }
}