namespace Eum.UI.ViewModels.Navigation;

public class NavigationState
{
    private NavigationState(
        INavigationStack<INavigatable> homeScreenNavigation)
    {
        HomeScreenNavigation = homeScreenNavigation;
    }

    public static NavigationState Instance { get; private set; } = null!;

    public INavigationStack<INavigatable> HomeScreenNavigation { get; }

    public static void Register(
        INavigationStack<INavigatable> homeScreenNavigation)
    {
        Instance = new NavigationState(
            homeScreenNavigation);
    }
}