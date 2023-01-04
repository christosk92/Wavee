using System.Linq;
using Eum.UI.ViewModels.Album;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Eum.UI.WinUI.Views.Album;

public partial class AlbumView : UserControl
{
    public AlbumViewModel ViewModel { get; }
    public AlbumView(AlbumViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    public string ToTrackCountString(int o)
    {
        return $"{o} tracks";
    }

    public object GetCorrectItemsSource(ICollectionView collectionView)
    {
        if (TracksSource.IsSourceGrouped)
        {
            return collectionView;
        }

        return ViewModel.Discs?.FirstOrDefault();
    }
}