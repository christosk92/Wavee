using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.Navigation;

public interface INavigate
{
    INavigationStack<RoutableViewModel> HomeScreen { get; }

    INavigationStack<RoutableViewModel> DialogScreen { get; }

    IObservable<bool> IsDialogOpen { get; }

    bool IsAnyPageBusy { get; }

    INavigationStack<RoutableViewModel> Navigate(NavigationTarget target);

    FluentNavigate To();

    Task<DialogResult<TResult>> NavigateDialogAsync<TResult>(DialogViewModelBase<TResult> dialog, NavigationTarget target = NavigationTarget.Default, NavigationMode navigationMode = NavigationMode.Normal);
}