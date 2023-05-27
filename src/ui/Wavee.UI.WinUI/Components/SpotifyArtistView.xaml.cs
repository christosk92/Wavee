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
using Eum.Spotify.context;
using LanguageExt;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Flyouts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class SpotifyArtistView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(SpotifyArtistView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(SpotifyArtistView), new PropertyMetadata(default(string), ImageChanged));

        private static void ImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spart = (SpotifyArtistView)d;
            if (e.NewValue is string s && !string.IsNullOrEmpty(s))
            {
                spart.MainImage.Source = new BitmapImage(new Uri(s));
            }
            else
            {
            }
        }

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(SpotifyArtistView), new PropertyMetadata(default(AudioId)));

        public SpotifyArtistView()
        {
            this.InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Image
        {
            get => (string)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public AudioId Id
        {
            get => (AudioId)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        private void SpotifyArtistView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
            {
                ButtonsPanel.Visibility = Visibility.Visible;
            }
        }

        private void SpotifyArtistView_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
            {
                ButtonsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void SpotifyArtistView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            Point point = new Point(0, 0);
            var properFlyout = Id.ConstructFlyout();
            if (args.TryGetPosition(sender, out point))
            {
                properFlyout.ShowAt(sender, point);
            }
            else
            {
                properFlyout.ShowAt((FrameworkElement)sender);
            }
        }

        private void MoreButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var properFlyout = Id.ConstructFlyout();
            properFlyout.ShowAt((FrameworkElement)sender);
        }

        private async void PlayButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var playContext = new PlayContextStruct(
                ContextId: Id.ToString(),
                Index: 0,
                TrackId: Option<AudioId>.None, 
                ContextUrl: $"context://{Id.ToString()}",
                NextPages: Option<IEnumerable<ContextPage>>.None,
                PageIndex: Option<int>.None);

            await ShellViewModel<WaveeUIRuntime>.Instance.Playback.PlayContextAsync(playContext);
        }
    }
}
