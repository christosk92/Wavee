using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.View.Home;

public sealed partial class HomeView : UserControl, ICacheablePage, INavigable
{
    public HomeView()
    {
        ViewModel = new HomeViewModel(ShellViewModel.Instance.User);
        _ = ViewModel.Fetch();
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


    private async void FilterClicked(object sender, ItemClickEventArgs e)
    {
        await Task.Delay(10);
        var selectedItem = (sender as TokenView)?.SelectedItem;
        if (selectedItem is null)
        {
            selectedItem = string.Empty;
        }
        if (selectedItem is string filter)
        {
            ViewModel.SelectedFilter = filter;
            await ViewModel.Fetch();
            ViewModel.SelectedFilter = filter;
        }
    }
}