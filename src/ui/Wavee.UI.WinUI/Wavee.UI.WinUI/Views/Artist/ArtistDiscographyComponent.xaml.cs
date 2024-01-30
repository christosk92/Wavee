using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistDiscographyComponent : UserControl
    {
        public static readonly DependencyProperty DiscographyProperty = DependencyProperty.Register(nameof(Discography), typeof(IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel>), typeof(ArtistDiscographyComponent), new PropertyMetadata(default(IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel>)));

        public ArtistDiscographyComponent()
        {
            this.InitializeComponent();
        }


        public IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> Discography
        {
            get => (IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel>)GetValue(DiscographyProperty);
            set => SetValue(DiscographyProperty, value);
        }
    }
}
