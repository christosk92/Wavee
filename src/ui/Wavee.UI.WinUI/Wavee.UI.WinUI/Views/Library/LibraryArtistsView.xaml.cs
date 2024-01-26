using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;


namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryArtistsView : UserControl
{
    public LibraryArtistsView(LibraryArtistsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    public LibraryArtistsViewModel ViewModel { get; }

    private void LibraryArtistsView_OnLoaded(object sender, RoutedEventArgs e)
    {
        this.Bindings.Update();
    }

    private void LibraryArtistsView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.Bindings.StopTracking();
    }
}