using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class AlbumLibraryPage : Page, INavigeablePage<LibraryAlbumsViewModel>
{
    public AlbumLibraryPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibraryAlbumsViewModel vm)
        {
            DataContext = vm;
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public LibraryAlbumsViewModel ViewModel => DataContext is LibraryAlbumsViewModel vm ? vm : null;
}
