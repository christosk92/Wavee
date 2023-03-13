using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Identity.User;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.ForYou.Recommended;

public class RecommendedViewModelFactory : SidebarItemViewModel
{
    public override string Icon => "\uE794";
    public override string Title => "Recommended";
    public override string GlyphFontFamily => "Segoe Fluent Icons";
    public override string Id => "recommended";
    public ServiceType ForService { get; init; }

    public override void NavigateTo()
    {
        switch (ForService)
        {
            case ServiceType.Spotify:
                NavigationService.Instance.To<SpotifyRecommendedViewModel>();
                break;
            case ServiceType.Local:
                NavigationService.Instance.To<LocalRecommendedViewModel>();
                break;
        }
    }
}