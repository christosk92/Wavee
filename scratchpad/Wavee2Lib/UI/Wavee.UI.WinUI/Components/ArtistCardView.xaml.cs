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
    public sealed partial class ArtistCardView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistCardView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(ArtistCardView), new PropertyMetadata(default(AudioId)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(ArtistCardView), new PropertyMetadata(default(string?), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ArtistCardView)d;
            if (e.NewValue is string image && !string.IsNullOrEmpty(image))
            {
                var bitmapImage = new BitmapImage();
                control.MainImage.Source = bitmapImage;
                bitmapImage.DecodePixelHeight = 200;
                bitmapImage.DecodePixelWidth = 200;
                bitmapImage.UriSource = new System.Uri(image, UriKind.RelativeOrAbsolute);
            }
        }

        public ArtistCardView()
        {
            this.InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public AudioId Id
        {
            get => (AudioId)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public string? Image
        {
            get => (string?)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
    }
}
