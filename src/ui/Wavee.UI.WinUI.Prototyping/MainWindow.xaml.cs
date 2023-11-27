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
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Application.Playback;
using Wavee.Spotify.Common.Contracts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Prototyping
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }

        private async void PlayTapped(object sender, TappedRoutedEventArgs e)
        {
            var uri = this.UriBox.Text;
            var spotifyClient = App.Sp.GetRequiredService<ISpotifyClient>();
            var source = SpotifyMediaSource.CreateFromUri(spotifyClient, uri, CancellationToken.None);
            await spotifyClient.Player.Play(source);
        }
    }
}
