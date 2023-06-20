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
using CommunityToolkit.WinUI.UI.Controls;
using Wavee.UI.Core.Contracts.Artist;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist.Overview
{
    public sealed partial class ArtistDiscographyHorizontalView : UserControl
    {
        public ArtistDiscographyHorizontalView(SpotifyArtistDiscographyV2[] items)
        {
            Items = items;
            this.InitializeComponent();
        }
        public SpotifyArtistDiscographyV2[] Items { get; }

        public int SelectedIndex
        {
            get;
            set;
        }


        // private void ElementFactory_OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        // {
        //     args.TemplateKey = "regular";
        // }

        private double incrementPerPage = 0;
        private void ArtistDiscographyHorizontalView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //calculate number of pages
            var width = e.NewSize.Width;
            var totalWidth = Scroller.ScrollableWidth;
            var pages = Math.Ceiling(totalWidth / width);
            incrementPerPage = totalWidth / pages;
            FlipViewPipsPager.NumberOfPages = (int)pages;
        }

        private double lastOffset = 0;
        private bool ignoreSetPage = false;
        private void FlipViewPipsPager_OnSelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args)
        {
            var page = sender.SelectedPageIndex;
            var offset = page * incrementPerPage;
            ignoreSetPage = true;
            lastOffset = offset;
            Scroller.ChangeView(offset, null, null);
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            this.FindAscendant<ArtistPage>()._storeditem = (sender as UIElement)
                .FindDescendant<ConstrainedBox>();
        }
    }
}
