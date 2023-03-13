using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.ForYou.Home;

public class HomeViewModelFactory : SidebarItemViewModel
{
    public override string Icon => "\uE10F";
    public override string Title => "Home";
    public override string GlyphFontFamily => "Segoe Fluent Icons";
    public override string Id => "home";
    public ServiceType ForService { get; init; }

    public override void NavigateTo()
    {
        switch (ForService)
        {
            case ServiceType.Local:
                NavigationService.Instance.To<LocalHomeViewModel>();
                break;
            case ServiceType.Spotify:
                NavigationService.Instance.To<HomeViewModel>();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}