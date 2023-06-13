using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Client.Artist;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist.Overview
{
    public sealed partial class ArtistDiscographyHorizontalView : UserControl
    {
        public ArtistDiscographyHorizontalView(List<ArtistDiscographyItem> items)
        {
            Items = items;
            this.InitializeComponent();
        }
        public List<ArtistDiscographyItem> Items { get; }


        private void ElementFactory_OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        {
            args.TemplateKey = "regular";
        }
    }
}
