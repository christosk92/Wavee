using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Aggregation;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;
using Eum.UI.ViewModels.Search.Sources;
using ReactiveUI;

namespace Eum.UI.ViewModels.Search;

[ObservableObject]
public partial class SearchBarViewModel
{
    private readonly ReadOnlyObservableCollection<SearchItemGroup> _groups;
    private readonly ReadOnlyObservableCollection<SearchGroup> _headers;
	[ObservableProperty] private bool _isSearchPageVisible;
	[ObservableProperty] private string _searchText = "";

	public SearchBarViewModel(IObservable<IChangeSet<ISearchItem, ComposedKey>> itemsObservable,
        IObservable<IChangeSet<SearchGroup>> sourceGroupChanges)
	{
		itemsObservable
			.Group(s => s.Category)
			.Transform(group => new SearchItemGroup(group.Key, group.Cache.Connect()))
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

    public ReadOnlyObservableCollection<SearchGroup> Headers => _headers;

    public ReadOnlyObservableCollection<SearchItemGroup> Groups => _groups;
}
