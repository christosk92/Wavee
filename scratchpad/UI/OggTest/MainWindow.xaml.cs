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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OggTest
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

        private MediaPlayer mp;
        private async void Mediaplayer_OnLoaded(object sender, RoutedEventArgs e)
        {
            mp = new MediaPlayer();
            mp.MediaFailed += MpOnMediaFailed;
            this.Mediaplayer.SetMediaPlayer(mp);
            var sample = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/sample.ogg"));

            var source = MediaSource.CreateFromStorageFile(sample);
            mp.Source = source;
            mp.Play();
        }

        private async void MpOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            var error = args.Error;
            var sample = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/sample.ogg"));

            var source = MediaSource.CreateFromStorageFile(sample);
            mp.Source = source;
            mp.Play();
        }
    }
}
