using Microsoft.UI.Xaml.Controls;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Home;

public sealed partial class HomeView : INavigationAwareView, ICacheablePage
{
    public HomeView()
    {
        this.InitializeComponent();
    }

    public void OnNavigatedTo(object parameter)
    {
        
    }

    public bool ShouldCache(int depth)
    {
        return depth <= 8;
    }

    public void RemovedFromCache()
    {

    }
}