using CommunityToolkit.WinUI.UI;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist.Sections.Grid
{
    public sealed partial class ArtistDiscographyGridView : UserControl
    {
        public ArtistDiscographyGridView(List<ArtistDiscographyView> artistDiscographyViews)
        {
            Items = artistDiscographyViews;
            this.InitializeComponent();
        }
        public List<ArtistDiscographyView> Items { get; }

        private void SpotifyItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag;
            if (tag is not AudioId id)
            {
                return;
            }

            //if the originalSource contains ButtonsPanel, we tapped on a button and we don't want to navigate
            if (e.OriginalSource is FrameworkElement originalSource
                && originalSource.FindAscendantOrSelf<StackPanel>(x => x.Name is "ButtonsPanel") is { })
            {
                return;
            }

            UICommands.NavigateTo.Execute(id);
        }
    }
}
