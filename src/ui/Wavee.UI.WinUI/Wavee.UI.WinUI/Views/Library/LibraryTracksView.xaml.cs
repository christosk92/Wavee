using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryTracksView : UserControl
{
    public LibraryTracksView(LibraryTracksViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    public LibraryTracksViewModel ViewModel { get; }
}