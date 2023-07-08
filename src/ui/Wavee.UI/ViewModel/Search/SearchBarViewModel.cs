using DynamicData.Binding;
using DynamicData;
using ReactiveUI;
using System.Reactive.Linq;
using Wavee.UI.ViewModel.Search.Patterns;
using System.Collections.ObjectModel;
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
            .Group(s => s.Category)
            .Transform(group => new SearchItemGroup(group.Key, group.Cache.Connect()))
            .Sort(SortExpressionComparer<SearchItemGroup>.Ascending(x => x.Title))
            .Bind(out _groups)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        HasResults = itemsObservable
            .Count()
            .Select(i => i > 0)
            .Replay(1)
            .ReplayLastActive();

        //bind hasresults to a property so we can use it in the view
        HasResults
            .ObserveOn(RxApp.MainThreadScheduler)
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
    string Description { get; }
    ComposedKey Key { get; }
    string? Icon { get; set; }
    string Category { get; }
    IEnumerable<string> Keywords { get; }
    bool IsDefault { get; }
}
