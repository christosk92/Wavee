using LanguageExt;
using Microsoft.UI.Xaml.Controls;

namespace ArtistTest.Sections.Grid
{
    public sealed partial class ArtistDiscographyGridView : UserControl
    {
        public ArtistDiscographyGridView(Seq<ArtistDiscographyView> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }
        public Seq<ArtistDiscographyView> Items { get; }
    }
}
