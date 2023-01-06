// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Enums;
using Eum.Spotify.connectstate;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.Spotify;
using Microsoft.Extensions.DependencyInjection;
using Eum.UI.Services.Users;
using Eum.UI.ViewModels;
using Eum.UI.Services.Login;
using Eum.Library.Logger.Helpers;
using Eum.Logging;
using Path = System.IO.Path;
using Eum.UI.Services.Directories;
using Eum.UI.Services.Playlists;
using Windows.Storage;
using Eum.Connections.Spotify.VLC;
using Eum.UI.Services.Artists;
using Eum.UI.Services.Tracks;
using LiteDB;
using Microsoft.UI;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using ReactiveUI;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using ColorThiefDotNet;
using Eum.Connections.Spotify.NAudio;
using Eum.UI.Helpers;
using Color = Windows.UI.Color;
using Eum.UI.Services.Albums;
using Windows.ApplicationModel.DataTransfer;
using LiteDB.Engine;
using Eum.UI.WinUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var dataDir = ApplicationData.Current.LocalFolder.Path;
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<MainViewModel>();

            //  serviceCollection.AddSingleton<IIdentityService, EumIdentityService>();
            serviceCollection.AddSingleton<IEumPlaylistManager, EumPlaylistManager>();
            serviceCollection.AddSingleton<IEumUserPlaylistViewModelManager, EumPlaylistViewModelManager>();

            var path = Path.Combine(dataDir, "data.db");
            var dbstring = $"filename={path}; journal=false";
            var db = new LiteDatabase(dbstring);
            
            serviceCollection.AddTransient<IFileHelper, WinUI_RandomAccessStream>();
            serviceCollection.AddSingleton<ILiteDatabase>(db);
            serviceCollection.AddTransient<ITrackAggregator, TrackAggregator>();
            serviceCollection.AddSingleton<IEumUserManager, EumUserManager>();
            serviceCollection.AddSingleton<IEumUserViewModelManager, EumUserViewModelManager>();
            serviceCollection.AddTransient<IArtistProvider, ArtistProvider>();
            serviceCollection.AddTransient<IAlbumProvider, AlbumProvider>();
            //serviceCollection.AddTransient<IUsersService, UsersService>();
            serviceCollection.AddTransient<IAvailableServicesProvider, BetaAvailableServicesProvider>();
            serviceCollection.AddSingleton<ICommonDirectoriesProvider>(new CommonDirectoriesProvider(dataDir));
            serviceCollection.AddTransient<IPlaybackService, PlaybackService>();
            serviceCollection.AddTransient<IErrorMessageShower, WinUI_ErrorMessageShower>();
            serviceCollection.AddTransient<IThemeSelectorServiceFactory, WinUiThemeSelectorServiceFactory>();

                serviceCollection.AddSpotify(new SpotifyConfig
            {
                AudioQuality = AudioQuality.VERY_HIGH,
                DeviceName = "Eum WinUI",
                DeviceType = DeviceType.Computer,
                AutoplayEnabled = true,
                CrossfadeDuration = (int)TimeSpan.FromSeconds(10).TotalMilliseconds,
                LogPath = Path.Combine(dataDir, "Logs_winui.log"),
                CachePath = dataDir,
                TimeSyncMethod = TimeSyncMethod.MELODY
            }, new EumVlcPlayer());

            this.UnhandledException += OnUnhandledException;
            Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
            this.InitializeComponent();
        }
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            S_Log.Instance.LogError(e.Message);
            S_Log.Instance.LogError(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MWindow = new MainWindow();
            MWindow.Activate();
        }


        public static Window MWindow { get; private set; }
    }

    public class WinUI_RandomAccessStream : IFileHelper
    {
        public async ValueTask<Stream> GetStreamForString(string playlistImagePath, CancellationToken cancellationToken)
        {
            var random = RandomAccessStreamReference.CreateFromUri(new Uri(playlistImagePath));
            IRandomAccessStream stream = await Task.Run(async () => await random.OpenReadAsync(), cancellationToken);
            return stream.AsStreamForRead();
        }
        
    }

    public class BetaAvailableServicesProvider : IAvailableServicesProvider
    {
        public ServiceType[] AvailableServices => new[]
        {
            ServiceType.Spotify
        };
    }
    // Helper class to workaround custom title bar bugs.
    // DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
    // https://github.com/microsoft/TemplateStudio/issues/4516
    internal class TitleBarHelper
    {
        private const int WAINACTIVE = 0x00;
        private const int WAACTIVE = 0x01;
        private const int WMACTIVATE = 0x0006;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        public static void UpdateTitleBar(ElementTheme theme)
        {
            if (App.MWindow.ExtendsContentIntoTitleBar)
            {
                if (theme != ElementTheme.Default)
                {
                    Application.Current.Resources["WindowCaptionForeground"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Colors.White),
                        ElementTheme.Light => new SolidColorBrush(Colors.Black),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };

                    Application.Current.Resources["WindowCaptionForegroundDisabled"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                        ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };

                    Application.Current.Resources["WindowCaptionButtonBackgroundPointerOver"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                        ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };

                    Application.Current.Resources["WindowCaptionButtonBackgroundPressed"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                        ElementTheme.Light => new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };

                    Application.Current.Resources["WindowCaptionButtonStrokePointerOver"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Colors.White),
                        ElementTheme.Light => new SolidColorBrush(Colors.Black),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };

                    Application.Current.Resources["WindowCaptionButtonStrokePressed"] = theme switch
                    {
                        ElementTheme.Dark => new SolidColorBrush(Colors.White),
                        ElementTheme.Light => new SolidColorBrush(Colors.Black),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };
                }

                Application.Current.Resources["WindowCaptionBackground"] = new SolidColorBrush(Colors.Transparent);
                Application.Current.Resources["WindowCaptionBackgroundDisabled"] = new SolidColorBrush(Colors.Transparent);

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MWindow);
                if (hwnd == GetActiveWindow())
                {
                    SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                    SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                }
                else
                {
                    SendMessage(hwnd, WMACTIVATE, WAACTIVE, IntPtr.Zero);
                    SendMessage(hwnd, WMACTIVATE, WAINACTIVE, IntPtr.Zero);
                }
            }
        }
    }
}
