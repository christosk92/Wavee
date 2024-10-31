using ReactiveUI;
using System.Reactive;
using System.Reactive.Disposables;
using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.AddUser;

[NavigationMetaData(
    Title = "Add User",
    Caption = "Create, connect, play",
    Order = 2,
    Category = "General",
    Keywords = new[]
        { "Wallet", "Add", "Create", "New", "Recover", "Import", "Connect", "Hardware", "ColdCard", "Trezor", "Ledger" },
    IconName = "nav_add_circle_24_regular",
    IconNameFocused = "nav_add_circle_24_filled",
    NavigationTarget = NavigationTarget.DialogScreen,
    NavBarPosition = NavBarPosition.Bottom,
    NavBarSelectionMode = NavBarSelectionMode.Button)]
public partial class AddUserPageViewModel : DialogViewModelBase<Unit>
{
    private AddUserPageViewModel()
    {

    }
    protected override void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
    {
        base.OnNavigatedTo(isInHistory, disposables);

        var enableCancel = UiContext.UserRepository.HasUser;
        SetupCancel(enableCancel: enableCancel, enableCancelOnEscape: enableCancel, enableCancelOnPressed: enableCancel);
    }

    public async Task Activate()
    {
        MainViewModel.Instance.IsOobeBackgroundVisible = true;
        await NavigateDialogAsync(this, NavigationTarget.DialogScreen);
        MainViewModel.Instance.IsOobeBackgroundVisible = false;
    }
}