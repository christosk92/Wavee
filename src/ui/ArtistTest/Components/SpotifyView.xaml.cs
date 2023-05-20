using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ArtistTest.Components
{
    public sealed partial class SpotifyView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SpotifyView), new PropertyMetadata(default(string)));

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
    }
}
