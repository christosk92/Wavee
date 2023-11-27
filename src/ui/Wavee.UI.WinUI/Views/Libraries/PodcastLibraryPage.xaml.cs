using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.WinUI.Contracts;


namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class PodcastLibraryPage : Page, INavigeablePage<LibraryPodcastsViewModel>
{
    public PodcastLibraryPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibraryPodcastsViewModel vm)
        {
            DataContext = vm;
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public LibraryPodcastsViewModel ViewModel => DataContext is LibraryPodcastsViewModel vm ? vm : null;
}