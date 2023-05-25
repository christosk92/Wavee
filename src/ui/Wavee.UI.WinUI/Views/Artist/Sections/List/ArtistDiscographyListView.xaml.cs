using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.WinUI.Views.Artist.Sections.List;

public sealed partial class ArtistDiscographyListView : UserControl
{
    public ArtistDiscographyListView(List<ArtistDiscographyView> artistDiscographyViews)
    {
        Items = artistDiscographyViews;
        this.InitializeComponent();
    }

    public List<ArtistDiscographyView> Items { get; }
}