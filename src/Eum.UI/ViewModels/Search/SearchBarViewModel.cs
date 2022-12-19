using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.PLinq;
using Eum.UI.Items;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using Eum.UI.ViewModels.Search.Sources;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

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
            .Group(s =>
            {
                return $"{s.Category}-{s.CategoryOrder}";
            })
            .Transform(group =>
            {
                var category2 = group.Key.Split('-');
                var category = category2[0];
                var order = int.Parse(category2[1]);
                var item = category switch
                {
                    "topHit" => new TopResultGroup(category, order, group.Cache.Connect()),
                    "topRecommendations" => new RecommendationsResultGroup(category, order, group.Cache.Connect()),
                    _ => group.Cache.Items.FirstOrDefault()?.Id.Type switch
                    {
                        EumEntityType.Track => new SongsResultGroup(category,
                            order,
                            group.Cache.Connect()),
                        EumEntityType.Artist => new ArtistResultGroup(category, order, group.Cache.Connect()),
                        _ => new SquareImageResultGroup(category, order, group.Cache.Connect()) as SearchItemGroup
                    }
                };
                return item;
            }, false)
            .SortBy(a => a.Order)
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
