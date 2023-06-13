using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Client.Artist;

namespace Wavee.UI.WinUI.Views.Artist.Overview
{
    public sealed partial class ArtistDiscographyGridView : UserControl
    {
        public ArtistDiscographyGridView(List<ArtistDiscographyItem> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }

        public List<ArtistDiscographyItem> Items { get; }

        private void ElementFactory_OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        {
            args.TemplateKey = "regular";
        }
    }
}
