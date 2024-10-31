using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;
using Wavee.ViewModels.ViewModels.Navigation;
using Wavee.ViewModels.ViewModels.Users;

namespace Wavee.ViewModels.ViewModels;

[AppLifetime]
public partial class MainViewModel : ViewModelBase
{
    [AutoNotify] private bool _isOobeBackgroundVisible;
    [AutoNotify] private WindowState _windowState;

    public MainViewModel(UiContext uiContext)
    {
        UiContext = uiContext;
        UiContext.SetMainViewModel(this);

        ApplyUiConfigWindowState();

        DialogScreen = new DialogScreenViewModel();
        NavBar = new NavBarViewModel(UiContext);
        MainScreen = new TargettedNavigationStack(NavigationTarget.Default);
        UiContext.RegisterNavigation(new NavigationState(UiContext, MainScreen, DialogScreen, NavBar));

        NavBar.Activate();

        NavigationManager.RegisterType(NavBar);

        this.RegisterAllViewModels(UiContext);

        RxApp.MainThreadScheduler.Schedule(async () => await NavBar.InitialiseAsync());

        this.WhenAnyValue(x => x.WindowState)
            .Where(state => state != WindowState.Minimized)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(state => UiContext.ApplicationSettings.WindowState = state);

        // IsMainContentEnabled =
        //     this.WhenAnyValue(
        //             x => x.DialogScreen.IsDialogOpen,
        //             x => x.FullScreen.IsDialogOpen,
        //             x => x.CompactDialogScreen.IsDialogOpen,
        //             (dialogIsOpen, fullScreenIsOpen, compactIsOpen) => !(dialogIsOpen || fullScreenIsOpen || compactIsOpen))
        //         .ObserveOn(RxApp.MainThreadScheduler);

        CurrentUser =
            this.WhenAnyValue(x => x.MainScreen.CurrentPage)
                .WhereNotNull()
                .OfType<UserViewModel>();

        IsOobeBackgroundVisible = UiContext.ApplicationSettings.Oobe;

        RxApp.MainThreadScheduler.Schedule(async () =>
        {
            if (!UiContext.UserRepository.HasUser || UiContext.ApplicationSettings.Oobe)
            {
                IsOobeBackgroundVisible = true;

                await UiContext.Navigate().To().WelcomePage().GetResultAsync();

                if (UiContext.UserRepository.HasUser)
                {
                    UiContext.ApplicationSettings.Oobe = false;
                    IsOobeBackgroundVisible = false;
                }
            }
            //
            // await Task.Delay(1000);
            //
            // foreach (var page in GetAnnouncements())
            // {
            //     await uiContext.Navigate().NavigateDialogAsync(page, navigationMode: NavigationMode.Clear);
            // }
        });


        Instance = this;
    }

    public TargettedNavigationStack MainScreen { get; }
    public DialogScreenViewModel DialogScreen { get; }
    public NavBarViewModel NavBar { get; }
    public IObservable<UserViewModel> CurrentUser { get; }

    public static MainViewModel Instance { get; private set; }
    public ReadOnlyObservableCollection<UserPageViewModel> Users { get; }
    public bool IsDialogOpen()
    {
        //TODO:
        return false;
    }

    public void ApplyUiConfigWindowState()
    {
        WindowState = UiContext.ApplicationSettings.WindowState;
    }

    public void ShowDialogAlert()
    {
        //TODO:
    }
}