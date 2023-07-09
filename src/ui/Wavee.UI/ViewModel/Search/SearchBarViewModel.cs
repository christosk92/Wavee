using DynamicData.Binding;
using DynamicData;
using ReactiveUI;
using System.Reactive.Linq;
using Wavee.UI.ViewModel.Search.Patterns;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows.Input;
using Wavee.UI.Common;

namespace Wavee.UI.ViewModel.Search;

public class SearchBarViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<FilterItem> _filters;
    private readonly ReadOnlyObservableCollection<SearchItemGroup> _groups;
    private bool _isSearchListVisible;
    private string? _searchText;
    private bool _hasResultsVal;
    private FilterItem? _selectedFilter;

    public SearchBarViewModel(
        IObservable<IChangeSet<ISearchItem, ComposedKey>> itemsObservable,
        IObservable<IChangeSet<FilterItem, string>> filtersObservable)
    {
        itemsObservable
            .SortBy(x => x.Category.Index)
            .Group(s => s.Category)
            .Transform(group => new SearchItemGroup(group.Key.Name,
                group.Key.Index,
                group.Cache.Connect()))
            .Sort(SortExpressionComparer<SearchItemGroup>.Ascending(x => x.CategoryIndex))
            .Bind(out _groups)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        filtersObservable
            .Bind(out _filters)
            .Do(_ =>
            {
                SelectedFilter = _filters.FirstOrDefault();
            })
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        HasResults = itemsObservable
            .Do(_ => Debug.WriteLine("Item changed in source observable"))
            .Catch((Exception ex) =>
            {
                Debug.WriteLine($"Error in observable sequence: {ex}");
                return Observable.Empty<IChangeSet<ISearchItem, ComposedKey>>();  // replace T with the appropriate type
            })
            .Scan(0, (count, _) => count + 1)
            .DistinctUntilChanged()
            .Select(i => i > 0)
            .Do(hasResults => Debug.WriteLine($"HasResults (before replay): {hasResults}"))
            .Replay(1)
            .RefCount()
            .Do(hasResults => Debug.WriteLine($"HasResults (after replay): {hasResults}"));

        HasResults
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(hasResults => Debug.WriteLine($"HasResults (on main thread): {hasResults}"))
            .BindTo(this, x => x.HasResultsVal);

        ResetCommand = ReactiveCommand.Create(ClearAndHideSearchList);
    }

    public bool IsSearchListVisible
    {
        get => _isSearchListVisible;
        set => this.RaiseAndSetIfChanged(ref _isSearchListVisible, value);
    }

    public FilterItem? SelectedFilter
    {
        get => _selectedFilter;
        set => this.RaiseAndSetIfChanged(ref _selectedFilter, value);
    }
    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ICommand ResetCommand { get; }

    public bool HasResultsVal
    {
        get => _hasResultsVal;
        set => this.RaiseAndSetIfChanged(ref _hasResultsVal, value);
    }
    public IObservable<bool> HasResults { get; }
    public ReadOnlyObservableCollection<SearchItemGroup> Groups => _groups;
    public ReadOnlyObservableCollection<FilterItem> Filters => _filters;
    private void ClearAndHideSearchList()
    {
        IsSearchListVisible = false;
        SearchText = "";
    }
}

public class FilterItem
{
    public string Id { get; init; }
    public string Title { get; init; }
    public long Count { get; init; }
    public bool IsNotOverview => Id is not "overview";

    public string FormatCount(long l)
    {
        //for example 1000000
        //would return as: 1,000,000
        return $"{l:N0}";
    }
}
public interface ISearchItem
{
    string Name { get; }
    ComposedKey Key { get; }
    CategoryComposite Category { get; }
    int ItemIndex { get; }

}

public readonly record struct CategoryComposite(string Name, int Index);
