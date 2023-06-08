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
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using Wavee.UI.WinUI.Components;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistOverviewView : UserControl
    {
        public ArtistOverviewView(IReadOnlyCollection<ArtistDiscographyGroupView> discography, IReadOnlyCollection<ArtistTopTrackView> topTracks)
        {
            Discography = discography;
            TopTracks = topTracks;
            this.InitializeComponent();
        }

        public IReadOnlyCollection<ArtistDiscographyGroupView> Discography { get; }
        public IReadOnlyCollection<ArtistTopTrackView> TopTracks { get; }

        public void ClearItems()
        {
            foreach (var item in this.FindDescendants()
                         .Where(c => c is ArtistDiscographyView)
                         .Cast<ArtistDiscographyView>())
            {
                item.ClearAll();
            }
        }

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
            if (args.IsContainerPrepared && args.ItemContainer.ContentTemplateRoot is TrackView f)
            {
                f.Index = index;
                f.ImageUrl = ((ArtistTopTrackView)args.Item).ReleaseImage;
            }
        }

        private void TopTracksContentContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var index = args.ItemIndex;
            if (args.ItemContainer?.ContentTemplateRoot is TrackView f)
            {
                f.Index = index;
                f.ImageUrl = ((ArtistTopTrackView)args.Item).ReleaseImage;
            }
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
        var items = TopTracks.Count;
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
    }
}
