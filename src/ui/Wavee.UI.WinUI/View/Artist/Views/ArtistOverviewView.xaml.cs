using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Client.Artist;
using Spotify.Metadata;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Artist.Views
{
    public sealed partial class ArtistOverviewView : UserControl
    {
        public ArtistOverviewView(WaveeUIArtistView waveeUiArtistView)
        {
            ArtistView = waveeUiArtistView;
            this.InitializeComponent();

            if (ArtistView.PreReleaseItem.IsSome)
            {
                PreReleaseOrPinnedItem.Content = ArtistView.PreReleaseItem.ValueUnsafe();
            }
            else if (ArtistView.PinnedItem.IsSome)
            {
                PreReleaseOrPinnedItem.Content = ArtistView.PinnedItem.ValueUnsafe();
            }
        }
        public WaveeUIArtistView ArtistView { get; set; }
        private void TopTracksGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer.SetVerticalScrollMode(TopTracksGrid, ScrollMode.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(TopTracksGrid, ScrollBarVisibility.Hidden);

            ScrollViewer.SetHorizontalScrollMode(TopTracksGrid, ScrollMode.Disabled);
            ScrollViewer.SetHorizontalScrollBarVisibility(TopTracksGrid, ScrollBarVisibility.Hidden);
        }
        private void TopTracksChoosingItemContainer(ListViewBase sender,
            ChoosingItemContainerEventArgs args)
        {
            var index = args.ItemIndex;
        }
        private void TopTracksContentContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var index = args.ItemIndex;
        }
        private void ArtistOverview_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = TopTracksGrid.ItemsPanelRoot as ItemsWrapGrid;
            static void SetStretchHorizontalAndNoWrap(ItemsWrapGrid wr, double width)
            {
                wr.Orientation = Orientation.Vertical;
                wr.ItemWidth = width;
                wr.MaximumRowsOrColumns = 5;
            }
            var expandReasonSize = e.NewSize.Width >= 800;
            if (!expandReasonSize)
            {
                SetStretchHorizontalAndNoWrap(panel, e.NewSize.Width);
                return;
            }
            //if we have more than 5 items, and the width >= 800, we make two columns
            var items = ArtistView?.TopTracks?.Length;
            if (items > 5)
            {
                panel.Orientation = Orientation.Vertical;
                panel.ItemWidth = e.NewSize.Width / 2;
                panel.MaximumRowsOrColumns = 5;
            }
            else
            {
                SetStretchHorizontalAndNoWrap(panel, e.NewSize.Width);
            }
        }

        private double _currentScrollPosition = 0;
        private async void Segments_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //if we tapped, we want to ignore the next scroll event
            var scroller = this.FindAscendant<ScrollViewer>();
            var offset = scroller.VerticalOffset;
            _currentScrollPosition = offset;
            scroller.VerticalScrollMode = ScrollMode.Disabled;
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            //scroller.ChangeView(null, _currentScrollPosition, null);

            // _currentScrollPosition = scroller.VerticalOffset;
            // //scroller.ViewChanged += ScrollerOnViewChanged;
            // scroller.ViewChanging += ScrollerOnViewChanging;
            await Task.Delay(500);
            scroller.VerticalScrollMode = ScrollMode.Enabled;
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            //scroller.ChangeView(null, _currentScrollPosition, null);
        }
    }
}
