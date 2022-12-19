using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using ReactiveUI;

namespace Eum.UI.ViewModels.Search;

[INotifyPropertyChanged]
public abstract partial class SearchItemGroup : IDisposable
{
    [ObservableProperty]
    private ISearchItem? _firstOrDefault;
	private readonly CompositeDisposable _disposables = new();
	private readonly ReadOnlyObservableCollection<ISearchItem> _items;

	public SearchItemGroup(string title, int order, IObservable<IChangeSet<ISearchItem, ComposedKey>> changes)
	{
		Title = title;
        Order = order;
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
    public SongsResultGroup(string title, int categoryOrder, IObservable<IChangeSet<ISearchItem, ComposedKey>> changes) : base(title, categoryOrder, changes)
    {
    }
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