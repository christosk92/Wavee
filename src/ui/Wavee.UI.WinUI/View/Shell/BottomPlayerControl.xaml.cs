using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Shell
{
    public sealed partial class BottomPlayerControl : UserControl
    {
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(UserViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(UserViewModel)));
        public static readonly DependencyProperty PlaybackProperty = DependencyProperty.Register(nameof(Playback), typeof(PlaybackViewModel), typeof(BottomPlayerControl), new PropertyMetadata(default(PlaybackViewModel)));

        public BottomPlayerControl()
        {
            this.InitializeComponent();
        }

        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public PlaybackViewModel Playback
        {
            get => (PlaybackViewModel)GetValue(PlaybackProperty);
            set => SetValue(PlaybackProperty, value);
        }

        public Visibility TrueIsCollapsed(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Image_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ExpandImageButton.Visibility = Visibility.Visible;
        }

        private void Image_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ExpandImageButton.Visibility = Visibility.Collapsed;
        }

        private void ExpandImageButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            User.Settings.ImageExpanded = !User.Settings.ImageExpanded;
        }
    }
}
