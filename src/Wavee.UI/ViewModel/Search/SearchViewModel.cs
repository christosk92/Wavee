using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.PLinq;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Core;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Core.Contracts.Search;

namespace Wavee.UI.ViewModel.Search;

//We use reactiveobject here because we want the throttling mechanism to be available
public sealed class SearchViewModel : ReactiveObject
{
    private readonly SourceCache<SearchResult, SearchResultKey> _sourceList = new(x => x.Key);
    private string? _query;
    private ReadOnlyObservableCollection<GroupedSearchResult> _results;
    private readonly CompositeDisposable _disposables;

    public SearchViewModel(IAppState appState)
    {
        var searchListener = this.WhenAnyValue(x => x.Query)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .SelectMany(async query => await appState.Search.SearchAsync(query, CancellationToken.None))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(result =>
            {
                _sourceList.Edit(innerList =>
                {
                    innerList.Clear();
                    innerList.AddOrUpdate(result);
                });
            });

        var mainListener = _sourceList.Connect()
            .Group(x => x.Key.Type)
            .Transform(group => new GroupedSearchResult(group))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _results)
            .Subscribe();

        _disposables = new CompositeDisposable(searchListener, mainListener);
    }

    public ReadOnlyObservableCollection<GroupedSearchResult> Results => _results;

    public string? Query
    {
        get => _query;
        set => this.RaiseAndSetIfChanged(ref _query, value);
    }
}

public class GroupedSearchResult
{
    public GroupedSearchResult(IGroup<SearchResult, SearchResultKey, SearchGroup> groupKey)
    {
        Group = groupKey.Key;
        Items = groupKey.Cache.Items.Select(c => c.Item).ToList();
        Title = groupKey.Key switch
        {
            SearchGroup.Highlighted => "Top result",
            SearchGroup.Recommended => "Recommended",
            SearchGroup.Track => "Tracks",
            SearchGroup.Album => "Albums",
            SearchGroup.Artist => "Artists",
            SearchGroup.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public SearchGroup Group { get; set; }
    public string Title { get; set; }
    public IReadOnlyList<CardItem> Items { get; set; }
}