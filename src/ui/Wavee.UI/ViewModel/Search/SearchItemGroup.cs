using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.UI.ViewModel.Search.Patterns;
using Wavee.UI.ViewModel.Search.Sources;

namespace Wavee.UI.ViewModel.Search;

public class SearchItemGroup : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReadOnlyObservableCollection<ISearchItem> _items;
    private readonly ReadOnlyObservableCollection<ISearchItem> _tracks;
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
    }
    public string Title { get; }
    public int CategoryIndex { get; }
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