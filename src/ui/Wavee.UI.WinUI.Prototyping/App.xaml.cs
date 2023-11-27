using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify;
using Eum.Spotify.connectstate;
using Wavee.Players.NAudio;
using Wavee.Spotify.Application.Authentication.Modules;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Prototyping
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Microsoft.UI.Xaml.Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            var storagePath = ApplicationData.Current.LocalFolder.Path;
            Directory.CreateDirectory(storagePath);
            Sp = new ServiceCollection()
                .AddSpotify(new SpotifyClientConfig
                {
                    Storage = new StorageSettings
                    {
                        Path = storagePath
                    },
                    Remote = new SpotifyRemoteConfig
                    {
                        DeviceName = "Wavee debug",
                        DeviceType = DeviceType.Computer
                    },
                    Playback = new SpotifyPlaybackConfig
                    {
                        InitialVolume = 0.5,
                        PreferedQuality = SpotifyAudioQuality.High
                    }
                })
                .WithStoredOrOAuthModule(OpenBrowser)
                .WithPlayer<NAudioPlayer>()
                .AddMediator()
                .BuildServiceProvider();
        }

        public static IServiceProvider Sp { get; private set; }

        private Task<OpenBrowserResult> OpenBrowser(string url, CancellationToken cancellationtoken)
        {
            if (m_window is null)
            {
                throw new InvalidOperationException("Window not initialized");
            }

            var tcs = new TaskCompletionSource<OpenBrowserResult>();
            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                var webView = new WebView2()
                {
                    Source = new Uri(url),
                    Width = 400,
                    Height = 600
                };
                //handle redirects
                webView.NavigationStarting += (_, args) =>
                {
                    if (args.Uri.ToString().StartsWith("http://127.0.0.1"))
                    {
                        tcs.SetResult(new OpenBrowserResult(

                            args.Uri, true));
                        //close
                        ((ContentDialog)webView.Parent).Hide();
                    }
                };


                var dialog = new ContentDialog
                {
                    Content = webView,
                    CloseButtonText = "Cancel",
                    XamlRoot = m_window.Content.XamlRoot
                };
                dialog.Closed += (_, _) =>
                {
                    // tcs.TrySetResult();
                };
                dialog.PrimaryButtonClick += (_, _) =>
                {
                    //tcs.TrySetResult(OpenBrowserResult.Success);
                };
                dialog.ShowAsync();
            });

            return tcs.Task;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private Window m_window;
    }
}
