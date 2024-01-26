using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;


namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryAlbumsView : UserControl
{
    public LibraryAlbumsView(LibraryAlbumsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    public LibraryAlbumsViewModel ViewModel { get; }
}