using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.Foundation;
using Eum.Spotify.context;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Flyouts;
using Wavee.UI.WinUI.Helpers;

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class SpotifyView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(SpotifyView), new PropertyMetadata(default(AudioId)));

        public SpotifyView()
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

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public AudioId Id
        {
            get => (AudioId)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        private void SpotifyView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
            {
                ButtonsPanel.Visibility = Visibility.Visible;
            }
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void SpotifyView_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
            {
                ButtonsPanel.Visibility = Visibility.Collapsed;
            }
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }

        private void SpotifyView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
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

        private void ButtonsPanel_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }

        private void ButtonsPanel_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void SpotifyView_OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            
        }
    }
}
