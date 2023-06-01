using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Home;

public sealed partial class HomeView : INavigationAwareView, ICacheablePage
{
    public HomeView()
    {
        ViewModel = new HomeViewModel();
        this.InitializeComponent();
    }
    public HomeViewModel ViewModel { get; }

    public void OnNavigatedTo(object parameter)
    {

    }

    public bool ShouldCache(int depth)
    {
        return depth <= 8;
    }

    public void RemovedFromCache()
    {
        ViewModel.Dispose();
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        
    }
}