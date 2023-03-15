using System.Collections.Generic;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.ForYou.Home;
using Button = Microsoft.UI.Xaml.Controls.Button;
using ComboBox = Microsoft.UI.Xaml.Controls.ComboBox;
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

        private void GridViewItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as GridViewItem).DataContext is AlbumViewModel v)
            {
                v.IsSelected = !v.IsSelected;
            }
        }

        private void SeeAllImportedTracksView_OnUnloaded(object sender, RoutedEventArgs e)
        {

        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var comboBox = sender as ComboBox;
                comboBox.SelectedItem = $"Sort by : {e.AddedItems[0]}";
            }
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.SearchForAlbum(sender.Text);
            }
        }
    }
}
