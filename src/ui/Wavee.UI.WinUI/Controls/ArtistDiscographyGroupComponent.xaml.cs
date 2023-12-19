using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Artist.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class ArtistDiscographyGroupComponent : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(ArtistViewDiscographyGroupViewModel), typeof(ArtistDiscographyGroupComponent), new PropertyMetadata(default(ArtistViewDiscographyGroupViewModel)));
        public ArtistDiscographyGroupComponent()
        {
            this.InitializeComponent();
        }

        public ArtistViewDiscographyGroupViewModel Item
        {
            get => (ArtistViewDiscographyGroupViewModel)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public bool IsEqualTo(int i, int s)
        {
            return i == s;
        }
    }
}
