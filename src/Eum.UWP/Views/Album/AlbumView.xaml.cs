using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Album;

namespace Eum.UWP.Views.Album;

public partial class AlbumView : UserControl
{
    public AlbumViewModel ViewModel { get; }
    public AlbumView(AlbumViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        this.DataContext = ViewModel;
    }
}
