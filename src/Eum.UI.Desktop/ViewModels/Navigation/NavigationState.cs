namespace Eum.UI.ViewModels.Navigation;

public class NavigationState
{
    private NavigationState(
        INavigationStack<RoutableViewModel> homeScreenNavigation)
    {
        HomeScreenNavigation = homeScreenNavigation;
    }

    public static NavigationState Instance { get; private set; } = null!;

    public INavigationStack<RoutableViewModel> HomeScreenNavigation { get; }


    public static void Register(
        INavigationStack<RoutableViewModel> homeScreenNavigation)
    {
        Instance = new NavigationState(
            homeScreenNavigation);
    }
}