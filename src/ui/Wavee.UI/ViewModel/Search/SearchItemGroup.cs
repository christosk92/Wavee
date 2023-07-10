using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.Id;
using Wavee.UI.Common;
using Wavee.UI.ViewModel.Search.Patterns;
using Wavee.UI.ViewModel.Search.Sources;

namespace Wavee.UI.ViewModel.Search;

public class SearchItemGroup : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<ISearchItem> _items;
    private readonly ReadOnlyObservableCollection<ISearchItem> _tracks;
    private readonly ReadOnlyObservableCollection<ICardViewModel> _asCards;
    private ISearchItem _firstItem;

    public SearchItemGroup(string title,
        int categoryIndex,
        Func<IObservable<IChangeSet<ISearchItem, ComposedKey>>> changesFactory)
    {
        Title = title;
        CategoryIndex = categoryIndex;
        changesFactory()
            .Sort(SortExpressionComparer<ISearchItem>.Ascending(x => x.ItemIndex))
            .Bind(out _items)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_disposables);

        if (categoryIndex is 0)
        {
            changesFactory()
                .Sort(SortExpressionComparer<ISearchItem>.Ascending(x => x.ItemIndex))
                .Filter(x => x is SpotifyTrackHit)
                .Bind(out _tracks)
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe()
                .DisposeWith(_disposables);

            _items.ToObservableChangeSet()
                .AutoRefresh()
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToCollection()
                .Select(x => x.FirstOrDefault())
                .BindTo(this, x => x.FirstItem)
                .DisposeWith(_disposables);
        }
        else
        {
            changesFactory()
                .Sort(SortExpressionComparer<ISearchItem>.Ascending(x => x.ItemIndex))
                .Transform(x => x switch
                {
                    SpotifyPlaylistHit playlist => new CardViewModel
                    {
                        Title = playlist.Name,
                        Id = playlist.SpotifyId.ToString(),
                        Image = playlist.Image,
                        Subtitle = playlist.Author,
                        Type = AudioItemType.Playlist
                    },
                    SpotifyArtistHit artist => new CardViewModel
                    {
                        Title = artist.Name,
                        Id = artist.SpotifyId.ToString(),
                        Image = artist.Image,
                        Subtitle = "Artist",
                        Type = AudioItemType.Artist,
                        IsArtist = true
                    },
                    SpotifyAlbumHit album => new CardViewModel
                    {
                        Title = album.Name,
                        Id = album.SpotifyId.ToString(),
                        Image = album.Image,
                        Subtitle = string.Join(", ", album.Artists.Select(z => z.Name)),
                        Type = AudioItemType.Album,
                        IsArtist = false
                    },
                    _ => new CardViewModel() as ICardViewModel,
                })
                .Bind(out _asCards)
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe()
                .DisposeWith(_disposables);
        }
    }
    public string Title { get; }
    public int CategoryIndex { get; }
    public ReadOnlyObservableCollection<ICardViewModel> AsCards => _asCards;
    public ReadOnlyObservableCollection<ISearchItem> Items => _items;
    public ReadOnlyObservableCollection<ISearchItem> OnlyTracks => _tracks;

    public ISearchItem FirstItem
    {
        get => _firstItem;
        set => this.RaiseAndSetIfChanged(ref _firstItem, value);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}