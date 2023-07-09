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
    private readonly ReadOnlyObservableCollection<SearchItemGroup> _groups;
    private bool _isSearchListVisible;
    private string? _searchText;
    private bool _hasResultsVal;

    public SearchBarViewModel(IObservable<IChangeSet<ISearchItem, ComposedKey>> itemsObservable)
    {
        itemsObservable
            .Group(s => (s.Category, s.CategoryIndex))
            .Transform(group => new SearchItemGroup(group.Key.Category,
                group.Key.CategoryIndex,
                group.Cache.Connect()))
            .Sort(SortExpressionComparer<SearchItemGroup>.Ascending(x => x.CategoryIndex))
            .Bind(out _groups)
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

    private void ClearAndHideSearchList()
    {
        IsSearchListVisible = false;
        SearchText = "";
    }
}
public interface ISearchItem
{
    string Name { get; }
    ComposedKey Key { get; }
    string Category { get; }
    int CategoryIndex { get; }
    int ItemIndex { get; }
}
