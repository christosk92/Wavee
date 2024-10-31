using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Providers;

namespace Wavee.ViewModels.ViewModels;

[AppLifetime]
public partial class ApplicationViewModel : ViewModelBase
{
    private readonly IMainWindowService _mainWindowService;
    [AutoNotify] private bool _isMainWindowShown = true;

    public ApplicationViewModel(UiContext uiContext, IMainWindowService mainWindowService)
    {
        _mainWindowService = mainWindowService;

        UiContext = uiContext;
        MainViewModel = new MainViewModel(UiContext);
    }

    public MainViewModel MainViewModel { get; }

    public bool CanShutdown(bool restart, out bool isShutdownEnforced)
    {
        isShutdownEnforced = Services.TerminateService.ForcefulTerminationRequestedTask.IsCompletedSuccessfully;

        if (!MainViewCanShutdown() && !restart)
        {
            return false;
        }

        return true;
    }

    public bool MainViewCanShutdown()
    {
        // Main view can shutdown when:
        // - no open dialog
        // - or no wallets available
        return !MainViewModel.Instance.IsDialogOpen()
               || !MainViewModel.Instance.Users.Any();
    }

    public void OnShutdownPrevented(bool restartRequest)
    {
        MainViewModel.Instance.ApplyUiConfigWindowState(); // Will pop the window if it was minimized.

        if (!MainViewCanShutdown() && !restartRequest)
        {
            MainViewModel.Instance.ShowDialogAlert();
            return;
        }

        // TODO: Ideally, we should be able to await GetResultAsync() to get the result from the Shutting Down dialog (whether the user cancelled the shutdown)
        // We can't do this right now because the state machine doesn't handle async.
        //
        // That would de-spaghettify this code, which is currently:
        //
        // - ApplicationViewModel.OnShutdownPrevented calls ShuttingDownDialog
        // - ShuttingDownDialog waits for shutdown conditions (unless user cancel) and calls ApplicationViewModel.ShutDown(), passing the restartRequest flag.
        // - So currently:
        //   - A is calling B, passing parameter X,
        //   - B is calling A back, passing parameter X back to A, which already had it in the first place.
        //
        // Instead of just A calling B, getting the result (just to determine if user cancelled) and proceeding with it's own logic.
        // This would also enable us to remove the dependency from ShuttingDownViewModel to ApplicationViewModel,
        // and even remove the Coinjoin stop/restart logic as well from there and place it here, where it really belongs.
//        UiContext.Navigate().To().ShuttingDown(this, restartRequest);
    }
}