using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
namespace Wavee.UI.WinUI.Views.Artist.About
{
    [ContentProperty(Name = "MainContent")]
    public sealed partial class ArtistAboutSection : UserControl
    {
        public ArtistAboutSection()
        {
            this.InitializeComponent();
        }

        public static DependencyProperty MainContentProperty =
            DependencyProperty.Register("MainContent", typeof(object),
                typeof(ArtistAboutSection), null);

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(ArtistAboutSection), new PropertyMetadata(default(IconElement)));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistAboutSection), new PropertyMetadata(default(string)));

        public object MainContent
        {
            get => GetValue(MainContentProperty);
            set => SetValue(MainContentProperty, value);
        }

        public IconElement Icon
        {
            get => (IconElement)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}
