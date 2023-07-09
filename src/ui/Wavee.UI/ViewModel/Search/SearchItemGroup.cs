using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search;

public class SearchItemGroup : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<ISearchItem> _items;

    public SearchItemGroup(string title,
        int categoryIndex,
        IObservable<IChangeSet<ISearchItem, ComposedKey>> changes)
    {
        Title = title;
        CategoryIndex = categoryIndex;
        changes
            .Sort(SortExpressionComparer<ISearchItem>.Ascending(x => x.ItemIndex))
            .Bind(out _items)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_disposables);
    }
    public string Title { get; }
    public int CategoryIndex { get; }
    public ReadOnlyObservableCollection<ISearchItem> Items => _items;

    public void Dispose()
    {
        _disposables.Dispose();
    }
}