using NeoSmart.AsyncLock;
using System.Collections.ObjectModel;
using Mediator;
using Wavee.UI.Features.Navigation.ViewModels;
using Wavee.UI.Features.Playlists.ViewModel;
using System.Collections.Immutable;
using Wavee.UI.Features.Playlists.Queries;
using Wavee.UI.Features.Tracks;
using Wavee.UI.Test;
using Wavee.UI.Features.Library.Queries;

namespace Wavee.UI.Features.Library.ViewModels;

public sealed class LibrarySongsViewModel : NavigationItemViewModel
{
    private bool _tracksLoaded;
    private string _generalSearchTerm = string.Empty;
    private readonly AsyncLock _lock = new AsyncLock();
    private TimeSpan? _totalDuration;

    private readonly IMediator _mediator;
    private readonly IUIDispatcher _uiDispatcher;

    public LibrarySongsViewModel(IMediator mediator, IUIDispatcher uiDispatcher)
    {
        _mediator = mediator;
        _uiDispatcher = uiDispatcher;
    }

    public bool TracksLoaded
    {
        get => _tracksLoaded;
        set => SetProperty(ref _tracksLoaded, value);
    }
    public ObservableCollection<LazyPlaylistTrackViewModel> Tracks { get; } = new();

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
            var trackIdsAndAttributes = await _mediator.Send(new GetLibrarySongsQuery
            {
                Offset = 0,
                Limit = int.MaxValue,
                Search = string.Empty,
                SortField = string.Empty
            });
            var tracks = trackIdsAndAttributes.Items.ToDictionary(x => x.Item, x => x);

            var tracksMetadata = await _mediator.Send(new GetTracksMetadataRequest
            {
                Ids = tracks.Select(f => f.Value.Item).ToImmutableArray(),
                SearchTerms = SearchTerms.Concat(new[]
                {
                    GeneralSearchTerm
                }).ToImmutableArray()
            });

            _uiDispatcher.Invoke(() =>
            {
                Tracks.Clear();
                int index = 0;
                TracksLoaded = true;
                double totalSeconds = 0;
                var tempTracks = new List<LazyPlaylistTrackViewModel>();
                foreach (var info in tracks.Values)
                {
                    if (tracksMetadata.TryGetValue(info.Item, out var track))
                    {
                        if (track.HasValue)
                        {
                            PlaylistTrackViewModel? trackasVm = null;
                            if (track.Value.Track is not null)
                            {
                                trackasVm = new PlaylistTrackViewModel(track.Value.Track, info);
                                totalSeconds += trackasVm.Duration.TotalSeconds;
                            }

                            tempTracks.Add(new LazyPlaylistTrackViewModel
                            {
                                HasValue = true,
                                Track = trackasVm!,
                                Index = index++
                            });
                        }
                    }
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