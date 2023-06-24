using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Wavee.UI.WinUI.View.Setup
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            this.InitializeComponent();
        }

        private async void OnIconLoaded(object sender, RoutedEventArgs e)
        {
            var player = (AnimatedVisualPlayer)sender;
            await player.PlayAsync(0, 0.5, false);
        }
    }
}
