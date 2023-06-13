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
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class TrackView : UserControl
    {
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(TrackView), new PropertyMetadata(default(int)));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(TrackView), new PropertyMetadata(default(AudioId)));
        public static readonly DependencyProperty AlternatingRowColorProperty = DependencyProperty.Register(nameof(AlternatingRowColor), typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(nameof(View), typeof(object), typeof(TrackView), new PropertyMetadata(default(object)));
        public static readonly DependencyProperty ImageUrlProperty =
            DependencyProperty.Register(nameof(ImageUrl),
                typeof(string), typeof(TrackView),
                new PropertyMetadata(default(string?), ImagePropertiesChanged));
        public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage),
            typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool), ImagePropertiesChanged));

        public TrackView()
        {
            this.InitializeComponent();
        }

        public int Index
        {
            get => (int)GetValue(IndexProperty);
            set => SetValue(IndexProperty, value);
        }
        public bool ShowImage
        {
            get => (bool)GetValue(ShowImageProperty);
            set => SetValue(ShowImageProperty, value);
        }

        public AudioId Id
        {
            get => (AudioId)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public bool AlternatingRowColor
        {
            get => (bool)GetValue(AlternatingRowColorProperty);
            set => SetValue(AlternatingRowColorProperty, value);
        }

        public object View
        {
            get => (object)GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }

        public string ImageUrl
        {
            get => (string)GetValue(ImageUrlProperty);
            set => SetValue(ImageUrlProperty, value);
        }

        private void HandleImagePropertiesChanged()
        {
            //ImageBorder
            //if showimage = false, unload ImageBorder and set MainContent relativepanel RightOf = savedbutton
            if (ShowImage)
            {
                var buttonsPanel = this.FindName("ImageBorder") as FrameworkElement;
                if (buttonsPanel != null)
                {
                    buttonsPanel.Visibility = Visibility.Visible;
                    buttonsPanel.Width = 28;
                }

                if (!string.IsNullOrEmpty(ImageUrl))
                {
                    var bitmapImage = new BitmapImage();
                    AlbumImage.Source = bitmapImage;
                    bitmapImage.DecodePixelHeight = 24;
                    bitmapImage.DecodePixelWidth = 24;
                    bitmapImage.UriSource = new System.Uri(ImageUrl, UriKind.RelativeOrAbsolute);

                    RelativePanel.SetRightOf(MainContent, "ImageBorder");
                }
            }
            else
            {
                //   var buttonsPanel = this.FindName("ButtonsPanel") as UIElement;
                if (ImageBorder != null)
                {
                    ImageBorder.Visibility = Visibility.Collapsed;
                    AlbumImage.Source = null;
                    Microsoft.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(ImageBorder);
                    RelativePanel.SetRightOf(MainContent, "SavedButton");
                }
            }
        }

        public string FormatIndex(int i)
        {
            //if we have 1, we want 01.
            //2 should be 02.
            //3 should be 03.
            var index = i + 1;
            var str = index.ToString("D2");
            return $"{str}.";
        }
        public Style GetStyleFor(int i)
        {
            return
                !AlternatingRowColor || (i % 2 == 0)
                    ? (Style)Application.Current.Resources["EvenBorderStyleGrid"]
                    : (Style)Application.Current.Resources["OddBorderStyleGrid"];
        }
        private static void ImagePropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (TrackView)d;
            x.HandleImagePropertiesChanged();
        }

        private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            var p = (AnimatedVisualPlayer)sender;
            if (!p.IsPlaying)
            {
                await p.PlayAsync(0, 1, true);
            }
        }

        private void PauseButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            
        }

        private void SavedButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            
        }
    }
}
