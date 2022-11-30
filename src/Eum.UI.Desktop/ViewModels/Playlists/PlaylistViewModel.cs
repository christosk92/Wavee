using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData.Binding;
using DynamicData;
using Eum.UI.Database;
using Eum.UI.Users;
using Eum.UI.ViewModels.Sidebar;
using Eum.UI.ViewModels.Track;
using Eum.Users;
using ReactiveUI;
using Eum.UI.ViewModels.Login;

namespace Eum.UI.ViewModels.Playlists;

public class PlaylistViewModel : SidebarItemViewModel, IComparable<PlaylistViewModel>
{
    private string _searchTerm;

    private IDisposable _tracksListDisposable;
    private readonly SourceList<PlaylistTrackViewModel> _tracksSourceList = new();
    private PlaylistSortFieldType _sortField;
    private SortDirection _sortDirection;
    private TimeSpan _totalTrackDuration;
    private bool _hasTracks;

    private PlaylistViewModel(EumPlaylist eumPlaylist)
    {
        Playlist = eumPlaylist;

        SortCommand = ReactiveCommand.Create<int>(type_int =>
        {
            var type = (PlaylistSortFieldType)type_int;
            if (SortField == type)
            {
                //switch next direction.
                //next in enum. iF we reached the end of the direction, default to "Index ascending"
                var current = (int) SortDirection;
                var next = (current + 1) % 2;
                _directionCount++;
                if (_directionCount == 3)
                {
                    _directionCount = 0;
                    SortDirection = SortDirection.Ascending;
                    SortField = PlaylistSortFieldType.Index;
                }
                else
                {
                    SortDirection = (DynamicData.Binding.SortDirection)next;
                }
            }
            else
            {
                //reset to descending as default direction
                _directionCount = 0;
                SortDirection = SortDirection.Descending;
                SortField = type;
                _directionCount++;
            }
        });
    }

    private IDisposable? _trackListenerDisposable;
    public void Connect()
    {
        Disconnect();
        
        
        var observableFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchTerm)
            .Select(BuildFilter);

        var sortFilter = this
            .WhenAnyValue(viewModel => viewModel.SortField, v => v.SortDirection)
            .Select(s => BuildSort(s.Item1, s.Item2));
        _trackListenerDisposable = _tracksSourceList.Connect()
            .Filter(observableFilter)
            .Sort(sortFilter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(Tracks)
            .Do(a =>
            {
                HasTracks = _tracksSourceList.Count > 0;
                TotalTrackDuration = TimeSpan.FromMilliseconds(_tracksSourceList.Items.Sum(a => a.Track.Duration.TotalMilliseconds));
                Playlist.Tracks = _tracksSourceList.Items.Select(a => a.Track.Id)
                    .ToArray();
            })
            .Subscribe();
        
        var tracksRepo = Ioc.Default.GetRequiredService<TracksRepository>();
        var tracks = tracksRepo.GetTracks(Playlist.Tracks ?? Array.Empty<Guid>());
        foreach (var eumPlaylistTrack in tracks)    
        {
            AddTrack(TrackViewModel.Create(eumPlaylistTrack));
        }
    }

    public void Disconnect()
    {
        _trackListenerDisposable?.Dispose();
    }
    
    private int _directionCount;
    public string SearchTerm
    {
        get => _searchTerm;
        set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
    }
    public PlaylistSortFieldType SortField
    {
        get => _sortField;
        set => this.RaiseAndSetIfChanged(ref _sortField, value);
    }
    public SortDirection SortDirection
    {
        get => _sortDirection;
        set => this.RaiseAndSetIfChanged(ref _sortDirection, value);
    }
    public int Order { get; }

    public static PlaylistViewModel Create(EumPlaylist eumPlaylist)
    {
        return new PlaylistViewModel(eumPlaylist);
    }

    public EumPlaylist Playlist { get; }

    public int CompareTo(PlaylistViewModel? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Order.CompareTo(other.Order);
    }

    public override string Title
    {
        get => Playlist.Name;
        protected set => throw new NotSupportedException();
    }
    public override string Glyph => "\uE93F";
    public override string GlyphFontFamily => "/Assets/MediaPlayerIcons.ttf#Media Player Fluent Icons";
    public ObservableCollectionExtended<PlaylistTrackViewModel> Tracks { get; } = new();

    public TimeSpan TotalTrackDuration
    {
        get => _totalTrackDuration;
        set => this.RaiseAndSetIfChanged(ref _totalTrackDuration, value);
    }

    public bool HasTracks
    {
        get => _hasTracks;
        set => this.RaiseAndSetIfChanged(ref _hasTracks, value);
    }

    public ICommand SortCommand { get; }

    private static Func<PlaylistTrackViewModel, bool> BuildFilter(string searchText)
    {
      //  return _ => true;
        if (string.IsNullOrEmpty(searchText)) return trade => true;
        return t =>
            t.Track.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || t.Track.Album.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || t.Track.Artists.Any(j => j.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private static SortExpressionComparer<PlaylistTrackViewModel> BuildSort(PlaylistSortFieldType sortFieldType, SortDirection direction)
    {
        IComparable Property(PlaylistTrackViewModel model)
        {
            return sortFieldType switch
            {
                PlaylistSortFieldType.Index => model.Index,
                PlaylistSortFieldType.Title => model.Track.Title,
                PlaylistSortFieldType.Artist => model.Track.Artists[0].Name,
                PlaylistSortFieldType.Album => model.Track.Album.Name,
                PlaylistSortFieldType.Added => true,
                PlaylistSortFieldType.Duration => model.Track.Duration,
                _ => throw new ArgumentOutOfRangeException(nameof(sortFieldType), sortFieldType, null)
            };
        }
        return direction switch
        {
            SortDirection.Ascending => SortExpressionComparer<PlaylistTrackViewModel>.Ascending(Property),
            SortDirection.Descending => SortExpressionComparer<PlaylistTrackViewModel>.Descending(Property)
        };
        //if (string.IsNullOrEmpty(searchText)) return trade => true;
        //return t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    public void AddTrack(TrackViewModel create)
    {
        _tracksSourceList.Add(new PlaylistTrackViewModel
        {
            Index = _tracksSourceList.Count,
            Track = create
        });
    }
}

public class PlaylistTrackViewModel
{
    public TrackViewModel Track { get; init; }
    public int Index { get; init; }

    public DateTime? Added { get; init; }
}

public enum PlaylistSortFieldType
{
    Index,
    Title,
    Artist,
    Album,
    Added,
    Duration
}