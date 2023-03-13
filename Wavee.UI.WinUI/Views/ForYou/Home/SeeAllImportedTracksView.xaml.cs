using System.Collections.Generic;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.ForYou.Home;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.ForYou.Home
{
    [ObservableObject]
    public sealed partial class SeeAllImportedTracksView : UserControl
    {
        [ObservableProperty]
        private int _view;
        public SeeAllImportedTracksView(SeeAllImportedTracksViewModel vm)
        {
            ViewModel = vm;
            IncrementalSource = new IncrementalLoadingCollection<FilesSource, TrackViewModel>(vm.Source, 50);
            // AlbumIncrementalSource = new IncrementalLoadingCollection<AlbumsSource, AlbumViewModel>(vm.AlbumsSource, 20);
            this.InitializeComponent();
        }
        public SeeAllImportedTracksViewModel ViewModel
        {
            get;
        }
        public IncrementalLoadingCollection<FilesSource, TrackViewModel> IncrementalSource
        {
            get;
        }
        // public IncrementalLoadingCollection<AlbumsSource, AlbumViewModel> AlbumIncrementalSource
        // {
        //     get;
        // }

        public Visibility HasItems(int b)
        {
            return b == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool IsInTracksView(int i)
        {
            return i == 1;
        }

        public bool IsInAlbumsView(int i)
        {
            return i == 0;
        }

        private void AlbumsGv_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
