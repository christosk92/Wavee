using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels;

public sealed partial class TitleBarViewModel : ReactiveObject
{
    private readonly ReadOnlyObservableCollection<TitleBarTabViewModel> _tabsView;
    [AutoNotify] private TitleBarTabViewModel? _activeTab;
    public TitleBarViewModel(LibraryViewModel library,
        AccountViewModel account,
        Home.HomeViewModel home,
        SearchViewModel search,
        IObservable<IChangeSet<TitleBarTabViewModel, Guid>> navigationTabs,
        IObservable<TitleBarTabViewModel> activeTabObservable)
    {
        Account = account;

        Library = TitleBarTabViewModel.Library(library);
        Home = TitleBarTabViewModel.Home(home);
        Search = TitleBarTabViewModel.Search(search);
        SignIn = TitleBarTabViewModel.SignIn(account);

        navigationTabs
            .Filter(x => x.PartOfMenuNavigation)
            .AutoRefresh(x => x.IsSelected)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _tabsView)
            .Subscribe(); ;

        ActiveTabObservable = activeTabObservable;
        activeTabObservable.Subscribe(x =>
        {
            foreach (var tab in _tabsView)
            {
                tab.IsSelected = tab == x;
            }

            ActiveTab = x;
        });
    }

    public TitleBarTabViewModel Library { get; }
    public TitleBarTabViewModel Home { get; }
    public TitleBarTabViewModel Search { get; }
    public TitleBarTabViewModel SignIn { get; }
    public AccountViewModel Account { get; }
    public ReadOnlyObservableCollection<TitleBarTabViewModel> TabsView => _tabsView;
    public IObservable<TitleBarTabViewModel> ActiveTabObservable { get; }
}