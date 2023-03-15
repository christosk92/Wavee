using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.Artist;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistRootView : UserControl
    {
        public ArtistRootViewModel ViewModel
        {
            get;
        }
        public ArtistRootView(ArtistRootViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
        }

        public Thickness GetTopMargin(double d, double extraTop,
            double bot,
            bool b)
        {
            return new Thickness(MainGrid.Margin.Left,
                ((b ? -d : d)) + extraTop,
                MainGrid.Margin.Right,
                bot);
        }

        public double GetHeight(double d)
        {
            return Math.Max(0, (d - 48));
        }

        private void GridView_SizeCHanged(object sender, SizeChangedEventArgs e)
        {
            var s = (sender as ListView);
            if (ViewModel.TopSongs is
                {
                    Length: > 5
                })
            {
                var columns = Math.Clamp(Math.Floor(s.ActualWidth / 300), 1, 2);
                ((ItemsWrapGrid)s.ItemsPanelRoot).ItemWidth = e.NewSize.Width / columns;
            }
            else
            {
                ((ItemsWrapGrid)s.ItemsPanelRoot).ItemWidth = e.NewSize.Width;
            }
        }

        private void MainGrid_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var verticalOffset = MainGrid.VerticalOffset;
            //check percentage to max
            var maxAt = InfoHeight.ActualHeight;
            if (maxAt > 0)
            {
                var percentage = Math.Clamp(verticalOffset / maxAt, 0, 1);
                TransitionControl.BlurValue = 50 * percentage;
                TransitionControl.Opacity = 1 - percentage;
            }
        }

        private void GridViewItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as GridViewItem).DataContext is AlbumViewModel v)
            {
                v.IsSelected = !v.IsSelected;
            }
        }
    }
}
