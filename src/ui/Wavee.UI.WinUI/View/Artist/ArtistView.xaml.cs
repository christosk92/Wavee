using System.Threading;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Artist;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.View.Artist;

public sealed partial class ArtistView : UserControl, INavigable, ICacheablePage
{
    public ArtistView()
    {
        this.InitializeComponent();
        ViewModel = new ArtistViewModel(ShellViewModel.Instance.User);
    }
    public ArtistViewModel ViewModel { get; }
    public async void NavigatedTo(object parameter)
    {
        if (parameter is string id)
        {
            await ViewModel.Fetch(id, CancellationToken.None);
        }
    }

    public void NavigatedFrom(NavigationMode mode)
    {

    }

    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 2;
    }

    public void RemovedFromCache()
    {

    }
}