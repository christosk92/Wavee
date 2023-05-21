using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist.Sections.List;

public sealed partial class ArtistDiscographyListView : UserControl
{
    public ArtistDiscographyListView(Seq<ArtistDiscographyView> artistDiscographyViews)
    {
        Items = artistDiscographyViews;
        this.InitializeComponent();
    }

    public Seq<ArtistDiscographyView> Items { get; }
}