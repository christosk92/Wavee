using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Eum.Spotify.playlist4;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using ReactiveUI;

namespace Eum.UI.ViewModels.Search;

[INotifyPropertyChanged]
public abstract partial class SearchItemGroup : IDisposable
{
    [ObservableProperty]
    private ISearchItem? _firstOrDefault;

    protected readonly CompositeDisposable _disposables = new();
	private readonly ReadOnlyObservableCollection<ISearchItem> _items;

	public SearchItemGroup(string title, int order, IObservable<IChangeSet<ISearchItem, ComposedKey>> changes)
	{
		Title = title;
        Order = order;
        if (changes != null)
        {
            changes
                .Bind(out _items)
                .DisposeMany()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(a =>
                {
                    FirstOrDefault = _items.FirstOrDefault();
                    return a;
                })
                .Subscribe()
                .DisposeWith(_disposables);
        }
    }
    public int Order { get; }
	public string Title { get; }


	public ReadOnlyObservableCollection<ISearchItem> Items => _items;

	public void Dispose()
	{
		_disposables.Dispose();
	}
}

public class TopResultGroup : SearchItemGroup
{
    public TopResultGroup(string title, 
        int order,
        IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, order, changes)
    {
    }
}

public class ArtistResultGroup : SearchItemGroup
{
    public ArtistResultGroup(string title, int categoryOrder, IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, categoryOrder, changes)
    {
    }
}

public class SongsResultGroup : SearchItemGroup
{
    private readonly ReadOnlyObservableCollection<PlaylistTrackViewModel> _tracks;

    public SongsResultGroup(string title, int categoryOrder, IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, categoryOrder, null)
    {
        int i = -1;
        changes
            .Select(a =>
            {
                i = -1;
                return a;
            })
            .Transform(a=>
            {
                i++;
                return new PlaylistTrackViewModel((a as SpotifyTrackSearchItem)!, i);
            })
            .Bind(out _tracks)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_disposables);
    }


    public ReadOnlyObservableCollection<PlaylistTrackViewModel> Tracks => _tracks;

}

public class RecommendationsResultGroup : SearchItemGroup
{
    public RecommendationsResultGroup(string title, int categoryOrder,
        IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, categoryOrder, changes)
    {
    }
}
public class SquareImageResultGroup : SearchItemGroup
{
    public SquareImageResultGroup(string title, int categoryOrder,
        IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, categoryOrder, changes)
    {
    }
}