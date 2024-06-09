using System;
using ReactiveUI;
using Wavee.UI.ViewModels.Library;
using System.Reactive.Linq;
using Wavee.Contracts.Interfaces;

namespace Wavee.UI.ViewModels;

public sealed partial class ShellViewModel : ReactiveObject
{
    public ShellViewModel(IViewFactory viewFactory, IAccountClientFactory accountClientFactory)
    {
        Account = new AccountViewModel(accountClientFactory);

        Navigation = new NavigationViewModel(viewFactory);

        Home = new Home.HomeViewModel(Account.Client);
        Search = new SearchViewModel();

        Library = new LibraryViewModel(Account.Library, Navigation.ActiveTabObservable);
        TitleBar = new TitleBarViewModel(
            Library,
            Account,
            Home,
            Search,
            Navigation.Tabs,
            Navigation.ActiveTabObservable);

        Navigation.Initialize(TitleBar.Home, TitleBar.Search, TitleBar.Library, TitleBar.SignIn);

        Player = new PlayerViewModel();

        Account
            .IsSignedInObservable
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                Guid tabId = x ? Constants.HomeTabId : Constants.SignInTabId;
                Navigation.MakeActive(tabId);
            });
    }

    public TitleBarViewModel TitleBar { get; }
    public LibraryViewModel Library { get; }
    public PlayerViewModel Player { get; }
    public AccountViewModel Account { get; }
    public Home.HomeViewModel Home { get; }
    public SearchViewModel Search { get; }

    public NavigationViewModel Navigation { get; }
    // public Task Initialize(Lazy<object> homeView, Lazy<object> searchView, Lazy<object> signInView)
    // {
    //     return Task.CompletedTask;
    // }
}