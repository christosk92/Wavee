using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist.Sections.Grid
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
