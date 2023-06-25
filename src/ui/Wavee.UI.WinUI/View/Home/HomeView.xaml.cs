using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.View.Home;

public sealed partial class HomeView : UserControl, ICacheablePage, INavigable
{
    public HomeView()
    {
        ViewModel = new HomeViewModel();
        this.InitializeComponent();
    }
    public HomeViewModel ViewModel { get; }
    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 10;
    }

    public void RemovedFromCache()
    {
        //clear data
    }

    public void NavigatedTo(object parameter)
    {
        
    }

    public void NavigatedFrom(NavigationMode mode)
    {

    }
}