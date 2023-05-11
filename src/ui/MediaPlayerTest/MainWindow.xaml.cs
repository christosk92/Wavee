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
using Windows.Media.Core;
using Windows.Media.Playback;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Wavee.Spotify;
using Wavee.Spotify.Configs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaPlayerTest
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void MediaPlayerElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            var cred = new LoginCredentials
            {
                Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
                AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
                Typ = AuthenticationType.AuthenticationUserPass
            };
            var cl = await SpotifyClient.Create(cred, new SpotifyConfig(
                Remote: new SpotifyRemoteConfig(
                    DeviceName: "Wavee WinUI",
                    DeviceType.Computer
                ),
                Playback: new SpotifyPlaybackConfig(
                    PreferredQualityType.High,
                    true
                )
            ), Option<ILogger>.None);
            var stream = await cl.Playback.Play("spotify:track:0afoCntatBcJGjz525RxBT");
            var rnd = stream.AsStream().AsRandomAccessStream();
            MediaPlayerElement.SetMediaPlayer(new MediaPlayer());
            MediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayerOnMediaFailed;
            MediaPlayerElement.MediaPlayer.Source = MediaSource.CreateFromStream(rnd, "audio/ogg");
            MediaPlayerElement.MediaPlayer.Play();
        }

        private void MediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {

        }
    }
}
