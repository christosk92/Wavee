using System;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Metadata.Artist;
using Wavee.UI.Client.Artist;

namespace Wavee.UI.WinUI.View.Artist.Views.Discography
{
    public sealed partial class DiscographyPageListView : UserControl
    {
        public DiscographyPageListView(GetReleases getReleasesFunc)
        {
            this.InitializeComponent();
            Releases = new IncrementalLoadingCollection<DiscographyReleasesSource, IArtistDiscographyRelease>(
                new DiscographyReleasesSource(getReleasesFunc));
            _ = Releases.LoadMoreItemsAsync(2);
            this.InitializeComponent();
        }

        public IncrementalLoadingCollection<DiscographyReleasesSource, IArtistDiscographyRelease> Releases { get; }

        private async void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate)
            {
                var scroller = (ScrollViewer)sender;
                var distanceToEnd = scroller.ExtentHeight - (scroller.VerticalOffset + scroller.ViewportHeight);

                // trigger if within 2 viewports of the end
                if (distanceToEnd <= 2.0 * scroller.ViewportHeight
                    && Releases.HasMoreItems && !Releases.IsLoading)
                {
                    await Releases.LoadMoreItemsAsync(2);
                }
            }
        }

        private void DiscographyPageGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var scroller = this.FindAscendant<ScrollViewer>();
            scroller.ViewChanged += Scroller_ViewChanged;
        }
    }
}