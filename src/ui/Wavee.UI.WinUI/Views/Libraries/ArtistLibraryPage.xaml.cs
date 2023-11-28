using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Libraries;

public sealed partial class ArtistLibraryPage : Page, INavigeablePage<LibraryArtistsViewModel>
{
    public ArtistLibraryPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is LibraryArtistsViewModel vm)
        {
            DataContext = vm;
            await vm.Initialize();
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public LibraryArtistsViewModel ViewModel => DataContext is LibraryArtistsViewModel vm ? vm : null;
}