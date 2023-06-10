using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;
using Microsoft.UI.Input;
using Wavee.UI.WinUI.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class CardView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(CardView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(CardView), new PropertyMetadata(default(AudioId)));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(CardView), new PropertyMetadata(default(string?)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(CardView), new PropertyMetadata(default(string?), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CardView)d;

            if (e.NewValue is string image && !string.IsNullOrEmpty(image))
            {
                var bitmapImage = new BitmapImage();
                control.LoadingImage.Source = bitmapImage;
                // bitmapImage.DecodePixelHeight = 200;
                // bitmapImage.DecodePixelWidth = 200;
                bitmapImage.UriSource = new System.Uri(image, UriKind.RelativeOrAbsolute);
            }
        }

        public CardView()
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

        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string? Image
        {
            get => (string?)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        private void CardView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var buttonsPanel = this.FindName("ButtonsPanel") as UIElement;
            if (buttonsPanel != null)
                buttonsPanel.Visibility = Visibility.Visible;

            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void CardView_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var buttonsPanel = this.FindName("ButtonsPanel") as UIElement;
            if (buttonsPanel != null)
            {
                buttonsPanel.Visibility = Visibility.Collapsed;
                Microsoft.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(buttonsPanel);
            }
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }
    }
}
