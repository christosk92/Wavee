using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Search;
using Wavee.UI.ViewModel.Search.Sources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components.Search
{
    public sealed partial class TopHitCard : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(ISearchItem), typeof(TopHitCard), new PropertyMetadata(default(ISearchItem), ItemChanged));

        private static void ItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (TopHitCard)d;
            x.OnitemChange(e.NewValue);
        }

        public TopHitCard()
        {
            this.InitializeComponent();
        }

        public ISearchItem Item
        {
            get => (ISearchItem)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }
        private void OnitemChange(object newValue)
        {
            switch (newValue)
            {
                case SpotifyArtistHit artist:
                    BackgroundImage.Source = artist.Image;
                    break;
                case SpotifyTrackHit track:
                    BackgroundImage.Source = track.Image;
                    break;
            }
        }
    }
}
