using LanguageExt;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ArtistTest.Sections.List
{
    public sealed partial class ArtistDiscographyListView : UserControl
    {
        public ArtistDiscographyListView(Seq<ArtistDiscographyView> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }

        public Seq<ArtistDiscographyView> Items { get; }
    }
}
