using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.ForYou;

public class HomeViewModel : INavigatable
{
    public void OnNavigatedTo(object parameter)
    {
            
    }

    public void OnNavigatedFrom()
    {
            
    }

    public int MaxDepth { get; }
}

public class HomeViewModelFactory : SidebarItemViewModel
{
    public override string Icon => "\uE10F";
    public override string Title => "Home";
    public override string GlyphFontFamily => "Segoe Fluent Icons";
    public override string Id => "home";

    public override void NavigateTo()
    {
        NavigationService.Instance.To<HomeViewModel>();
    }
}