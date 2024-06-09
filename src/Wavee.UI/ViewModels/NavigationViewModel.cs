using System;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels;

public sealed partial class NavigationViewModel : ReactiveObject
{
    private readonly SourceCache<TitleBarTabViewModel, Guid> _tabs = new(x => x.Id);
    [AutoNotify] private TitleBarTabViewModel? _activeTab;
    [AutoNotify] private object? _currentView;

    private readonly IViewFactory _viewFactory;

    // private ViewTab? _homeTab;
    // private ViewTab? _searchTab;
    // private ViewTab? _signInTab;
    public NavigationViewModel(IViewFactory viewFactory)
    {
        Instance = this;
        _viewFactory = viewFactory;
        Tabs = _tabs
            .Connect()
            .AutoRefresh(x => x.IsSelected);

        var selectedTabChanged = this
            .WhenAnyValue(x => x.ActiveTab)
            .Where(x => x != null);

        ActiveTabObservable = selectedTabChanged;

        // Refresh on tab change, but also refresh on tab's viewmodel property (Tab.ViewModel)
        var tabChanged = this
            .WhenAnyValue(x => x.ActiveTab)
            .Where(x => x != null)
            .Select(tab => tab!);

        // Observable for active tab's view model changes
        var viewModelChanged = tabChanged
            .Select(tab => tab.ViewModelObservable)
            .Switch();

        var combinedObservable = tabChanged.CombineLatest(viewModelChanged,
            (tab, viewModel) => (tab, viewModel)
        );

        combinedObservable
            .Subscribe(tuple =>
            {
                var (tab, viewModel) = tuple;
                UpdateView(tab, viewModel);
            });
    }

    private void UpdateView(TitleBarTabViewModel tab, object viewModel)
    {
        var view = _viewFactory.CreateView(viewModel);
        if (view is not null)
        {
            CurrentView = view;
        }
    }

    public IObservable<IChangeSet<TitleBarTabViewModel, Guid>> Tabs { get; }
    public IObservable<TitleBarTabViewModel> ActiveTabObservable { get; }

    //TODO: Remove static reference 
    public static NavigationViewModel Instance { get; private set; }

    public void Initialize(TitleBarTabViewModel homeTab,
        TitleBarTabViewModel searchTab,
        TitleBarTabViewModel libraryTab,
        TitleBarTabViewModel signInTab)
    {
        _tabs.AddOrUpdate(homeTab);
        _tabs.AddOrUpdate(searchTab);
        _tabs.AddOrUpdate(libraryTab);
        _tabs.AddOrUpdate(signInTab);
    }

    public Guid CreateTab(TitleBarTabViewModel tab)
    {
        // cannot make home and search tabs!
        if (tab.Id == Constants.HomeTabId || tab.Id == Constants.SearchTabId || tab.Id == Constants.LibraryTabId ||
            tab.Id == Constants.SignInTabId)
            throw new InvalidOperationException("Cannot create home or search tabs");

        _tabs.AddOrUpdate(tab);
        return tab.Id;
    }

    public TitleBarTabViewModel? GetTab(Guid id)
    {
        return _tabs.Lookup(id).Value;
    }

    public void MakeActive(Guid id)
    {
        var tab = GetTab(id);
        if (tab is null) return;

        ActiveTab = tab;
    }
}

// public sealed class ViewItem
// {
//     private object _page;
//
//     private ViewItem(object item, ViewTab tab, object viewModel, bool realized)
//     {
//         Page = item;
//         Tab = tab;
//         PageRealized = realized;
//         ViewModel = viewModel;
//     }
//
//     public static ViewItem Full(object page, object viewmodel, ViewTab tab) => new(page, tab, viewmodel, true);
//
//     public static ViewItem Lazy(Lazy<object> pageLazy, object viewmodel, ViewTab tab) =>
//         new(pageLazy, tab, viewmodel, false);
//
//     public object ViewModel { get; }
//
//     public object Page
//     {
//         get
//         {
//             if (_page is Lazy<object> lazy)
//             {
//                 _page = lazy.Value;
//                 PageRealized = true;
//             }
//
//             return _page;
//         }
//         private set => _page = value;
//     }
//
//     public ViewTab? Tab { get; }
//     public bool PageRealized { get; private set; }
// }
//
// public sealed partial class ViewTab : ReactiveObject, IDisposable
// {
//     [AutoNotify] private bool _isActive;
//     [AutoNotify] private ViewItem? _current;
//
//     private readonly IDisposable _disposable;
//
//     public ViewTab(TitleBarTabViewModel viewModel, IViewFactory viewFactory)
//     {
//         ViewModel = viewModel;
//         Backstack = new();
//
//         var composite = new CompositeDisposable();
//         // var viewModelChanged = ViewModel.WhenAnyValue(x => x.ViewModel);
//         // var thisBecameActive = this.WhenAnyValue(x => x.IsActive).Where(x => x);
//         // var combined = viewModelChanged.CombineLatest(thisBecameActive, (vm, y) => (vm, y));
//         //
//         // combined
//         //     .Subscribe(f =>
//         //     {
//         //         if (!f.y)
//         //         {
//         //             return;
//         //         }
//         //         Current = ViewItem.Full(viewFactory.CreateView(f.vm), f.vm, this);
//         //     })
//         //     .DisposeWith(composite);
//         //
//         // ViewModel
//         //     .WhenAnyValue(x => x.IsSelected)
//         //     .Skip(1)
//         //     .Subscribe(f =>
//         //     {
//         //         IsActive = f;
//         //     })
//         //     .DisposeWith(composite);
//
//
//
//         _disposable = composite;
//     }
//
//     public Stack<ViewItem> Backstack { get; }
//     public TitleBarTabViewModel ViewModel { get; }
//
//     public void Dispose()
//     {
//         _disposable.Dispose();
//     }
//
//     public void NavigateTo(ViewItem item)
//     {
//         if (item.Tab != this) return;
//
//         if (Current != null)
//         {
//             Backstack.Push(Current);
//         }
//
//         Current = item;
//     }
// }