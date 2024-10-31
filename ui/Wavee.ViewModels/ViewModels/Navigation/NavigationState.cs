using System.Reactive.Linq;
using ReactiveUI;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;
using Wavee.ViewModels.ViewModels.Users;

namespace Wavee.ViewModels.ViewModels.Navigation;

[AppLifetime]
public class NavigationState : ReactiveObject, INavigate
{
    private readonly IUserNavigation _userNavigation;

    public NavigationState(
        UiContext uiContext,
        INavigationStack<RoutableViewModel> homeScreenNavigation,
        INavigationStack<RoutableViewModel> dialogScreenNavigation,
        IUserNavigation userNavigation)
    {
        UiContext = uiContext;
        HomeScreen = homeScreenNavigation;
        DialogScreen = dialogScreenNavigation;
        _userNavigation = userNavigation;
        this.WhenAnyValue(
                x => x.DialogScreen.CurrentPage,
                x => x.HomeScreen.CurrentPage,
                (dialog, mainScreen) => dialog ?? mainScreen)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(OnCurrentPageChanged)
            .Subscribe();

        IsDialogOpen =
            this.WhenAnyValue(
                x => x.DialogScreen.CurrentPage,
                x => x.HomeScreen.CurrentPage,
                (d, _) => d != null);
    }

    public UiContext UiContext { get; }

    public INavigationStack<RoutableViewModel> HomeScreen { get; }

    public INavigationStack<RoutableViewModel> DialogScreen { get; }
    public IObservable<bool> IsDialogOpen { get; }

    public bool IsAnyPageBusy =>
        HomeScreen.CurrentPage is { IsBusy: true } ||
        DialogScreen.CurrentPage is { IsBusy: true };

    public INavigationStack<RoutableViewModel> Navigate(NavigationTarget currentTarget)
    {
        return currentTarget switch
        {
            NavigationTarget.HomeScreen => HomeScreen,
            NavigationTarget.DialogScreen => DialogScreen,
            _ => throw new NotSupportedException(),
        };
    }

    public FluentNavigate To()
    {
        return new FluentNavigate(UiContext);
    }

    public UserViewModel? To(UserModel user)
    {
        return _userNavigation.To(user);
    }

    public async Task<DialogResult<TResult>> NavigateDialogAsync<TResult>(DialogViewModelBase<TResult> dialog, NavigationTarget target = NavigationTarget.Default, NavigationMode navigationMode = NavigationMode.Normal)
    {
        target = NavigationExtensions.GetTarget(dialog, target);
        return await Navigate(target).NavigateDialogAsync(dialog, navigationMode);
    }

    private void OnCurrentPageChanged(RoutableViewModel page)
    {
        if (HomeScreen.CurrentPage is { } homeScreen)
        {
            homeScreen.IsActive = false;
        }

        if (DialogScreen.CurrentPage is { } dialogScreen)
        {
            dialogScreen.IsActive = false;
        }

        page.IsActive = true;
    }
}
