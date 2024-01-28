using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.ViewModels.Artist;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
