using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Client.Artist;
using Wavee.UI.WinUI.Helpers;
using UIElement = Microsoft.UI.Xaml.UIElement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist.Overview
{
    public sealed partial class ArtistDiscographyListView : UserControl
    {
        public ArtistDiscographyListView(List<ArtistDiscographyItem> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }
        public List<ArtistDiscographyItem> Items { get; }
        private void ElementFactory_OnSelectTemplateKey(RecyclingElementFactory sender, SelectTemplateEventArgs args)
        {
            args.TemplateKey = args.DataContext switch
            {
                ArtistDiscographyTrack => "track",
                _ => "regular"
            };
        }

        private void UIElement_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void UIElement_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }
    }
}
