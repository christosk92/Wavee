using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ArtistTest
{
    public sealed partial class ArtistOverview : UserControl
    {
        public ArtistOverview(ref ArtistView artistPage)
        {
            Artist = artistPage;
            this.InitializeComponent();
            GC.Collect();
        }
        public ArtistView Artist { get; }

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
            var items = Artist.TopTracks.Length;
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

        private void TopTracksGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer.SetVerticalScrollMode(TopTracksGrid, ScrollMode.Disabled);
            ScrollViewer.SetVerticalScrollBarVisibility(TopTracksGrid, ScrollBarVisibility.Hidden);

            ScrollViewer.SetHorizontalScrollMode(TopTracksGrid, ScrollMode.Disabled);
            ScrollViewer.SetHorizontalScrollBarVisibility(TopTracksGrid, ScrollBarVisibility.Hidden);

        }
    }
}
