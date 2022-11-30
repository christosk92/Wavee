using System.Reactive.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels.Login;
using Eum.UI.ViewModels.Navigation;
using ReactiveUI;

namespace Eum.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private bool _isLoginVisible = true;

    public MainViewModel(ProfilesViewModel profilesViewModel,
        UserManagerViewModel userManagerViewModel)
    {
        ProfilesViewModel = profilesViewModel;

        MainScreen = new TargettedNavigationStack(NavigationTarget.HomeScreen);
        NavigationState.Register(MainScreen);

        userManagerViewModel.UserSelected += (sender, user) =>
        {
            IsLoginVisible = user == null;
        };

        this.WhenAnyValue(
                x => x.MainScreen.CurrentPage,
                _ => _,
                (mainScreen, _) => mainScreen)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(page => page.SetActive())
            .Subscribe();

    }
    public IObservable<bool> IsMainContentEnabled { get; }
    public TargettedNavigationStack MainScreen { get; }

    public bool IsLoginVisible
    {
        get => _isLoginVisible;
        set => this.RaiseAndSetIfChanged(ref _isLoginVisible, value);
    }

    public ProfilesViewModel ProfilesViewModel { get; }
    public ICommand ItemInvokedCommand { get; }
}