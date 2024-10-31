using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.ViewModels;
using Wavee.ViewModels.ViewModels.NavBar;
using Wavee.ViewModels.ViewModels.Navigation;

namespace Wavee.ViewModels.Models.UI;

public class UiContext
{
    /// <summary>
    ///     The use of this property is a temporary workaround until we finalize the refactoring of all ViewModels (to be
    ///     testable)
    /// </summary>
    public static UiContext Default;

    private INavigate? _navigate;

    public UiContext(
        IApplicationSettings applicationSettings,
        IUserRepository userRepository)
    {
        ApplicationSettings = applicationSettings;
        UserRepository = userRepository;
        Default = this;
    }

    public MainViewModel? MainViewModel { get; private set; }
    public IApplicationSettings ApplicationSettings { get; }
    public IUserRepository UserRepository { get; }

    public void SetMainViewModel(MainViewModel viewModel)
    {
        MainViewModel ??= viewModel;
    }

    public INavigate Navigate()
    {
        return _navigate ??
               throw new InvalidOperationException($"{GetType().Name} {nameof(Navigate)} hasn't been initialized.");
    }

    public INavigationStack<RoutableViewModel> Navigate(NavigationTarget target)
    {
        return
            _navigate?.Navigate(target)
            ?? throw new InvalidOperationException($"{GetType().Name} {nameof(Navigate)} hasn't been initialized.");
    }
    public void RegisterNavigation(INavigate navigate)
    {
        _navigate ??= navigate;
    }
}