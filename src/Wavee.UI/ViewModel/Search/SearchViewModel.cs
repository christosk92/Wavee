using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
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
    private readonly ReadOnlyObservableCollection<GroupedSearchResult> _results;
    private readonly CompositeDisposable _disposables;
    private GroupedSearchResult _firstItem;
    private GroupedSearchResult _secondItem;

    public SearchViewModel(IAppState appState)
    {
        var mainListener = _sourceList
            .Connect()
            .Group(x => x.Key.Type)
            .Transform(group => new GroupedSearchResult(group))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(Results)
            .DisposeMany()
            //.DisposeMany()
            .Subscribe();

        var searchListener = this.WhenAnyValue(x => x.Query)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .SelectMany(async query =>
            {
                try
                {
                    return await appState.Search.SearchAsync(query, CancellationToken.None);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return Enumerable.Empty<SearchResult>();
                }
            })
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(result =>
            {
                _sourceList.Clear();
                _sourceList.AddOrUpdate(result);
                // _sourceList.Edit(innerList =>
                // {
                //     innerList.Clear();
                //     innerList.AddOrUpdate(result);
                // });
            });

        _disposables = new CompositeDisposable(searchListener, mainListener);
    }

    public ObservableCollectionExtended<GroupedSearchResult> Results { get; } =
        new ObservableCollectionExtended<GroupedSearchResult>();

    public string? Query
    {
        get => _query;
        set => this.RaiseAndSetIfChanged(ref _query, value);
    }
    //
    // public GroupedSearchResult FirstItem
    // {
    //     get => _firstItem;
    //     set => this.RaiseAndSetIfChanged(ref _firstItem, value);
    // }
    //
    // public GroupedSearchResult SecondItem
    // {
    //     get => _secondItem;
    //     set => this.RaiseAndSetIfChanged(ref _secondItem, value);
    // }
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
            SearchGroup.PodcastEpisode => "Podcast episodes",
            SearchGroup.Playlist => "Playlists",
            SearchGroup.PodcastShow => "Podcast shows",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public GroupedSearchResult(IGrouping<SearchGroup, SearchResult> groupKey)
    {
        Group = groupKey.Key;
        Items = groupKey.Select(c => c.Item).ToList();
        Title = groupKey.Key switch
        {
            SearchGroup.Highlighted => "Top result",
            SearchGroup.Recommended => "Recommended",
            SearchGroup.Track => "Tracks",
            SearchGroup.Album => "Albums",
            SearchGroup.Artist => "Artists",
            SearchGroup.Unknown => "Unknown",
            SearchGroup.PodcastEpisode => "Podcast episodes",
            SearchGroup.Playlist => "Playlists",
            SearchGroup.PodcastShow => "Podcast shows",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public SearchGroup Group { get; set; }
    public string Title { get; set; }
    public IReadOnlyList<CardItem> Items { get; set; }
}