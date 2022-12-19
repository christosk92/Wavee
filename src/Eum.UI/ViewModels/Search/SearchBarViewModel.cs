using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Aggregation;
using Eum.UI.Items;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using Eum.UI.ViewModels.Search.Sources;
using ReactiveUI;

namespace Eum.UI.ViewModels.Search;

[ObservableObject]
public partial class SearchBarViewModel
{
    private readonly ReadOnlyObservableCollection<SearchItemGroup> _groups;
    private readonly ReadOnlyObservableCollection<ISearchGroup> _headers;
    [ObservableProperty] private bool _isSearchPageVisible;
    [ObservableProperty] private string _searchText = "";

    public SearchBarViewModel(IObservable<IChangeSet<ISearchItem, ComposedKey>> itemsObservable,
        IObservable<IChangeSet<ISearchGroup>> sourceGroupChanges)
    {
        itemsObservable
            .SortBy(a => a.CategoryOrder)
            .Group(s => s.Category)
            .Where(a=> a.Count > 0)
            .Transform(group =>
            {
                return group.Key switch
                {
                    "topHit" => new TopResultGroup(group.Key, group.Cache.Items.FirstOrDefault()?.CategoryOrder ?? 0, group.Cache.Connect()),
                    "topRecommendations" => new RecommendationsResultGroup(group.Key, group.Cache.Items.First().CategoryOrder, group.Cache.Connect()),
                    _ => group.Cache.Items.First().Id.Type switch
                    {
                        EumEntityType.Track => new SongsResultGroup(group.Key,
                            group.Cache.Items.First().CategoryOrder,
                            group.Cache.Connect()),
                        EumEntityType.Artist => new ArtistResultGroup(group.Key, group.Cache.Items.First().CategoryOrder,group.Cache.Connect()),
                        _ => new SquareImageResultGroup(group.Key, group.Cache.Items.First().CategoryOrder, group.Cache.Connect()) as SearchItemGroup
                    }
                };
            })
            .SortBy(a=> a.Order)
            .Bind(out _groups)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();

        sourceGroupChanges
            .Bind(out _headers)
            .DisposeMany()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();


        HasResults = itemsObservable
            .Count()
            .Select(i => i > 0)
            .Replay(1)
            .RefCount();
    }

    public IObservable<bool> HasResults { get; }

    public ReadOnlyObservableCollection<ISearchGroup> Headers => _headers;

    public ReadOnlyObservableCollection<SearchItemGroup> Groups => _groups;
}
