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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.NAudio;
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
using Eum.UI.Users;
using Windows.Storage;
using Eum.UI.Services.Tracks;
using LiteDB;

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
            S_Log.Instance.InitializeDefaults(Path.Combine(dataDir, "Logs.txt"), null);

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<MainViewModel>();

          //  serviceCollection.AddSingleton<IIdentityService, EumIdentityService>();
            serviceCollection.AddSingleton<IEumPlaylistManager, EumPlaylistManager>();
            serviceCollection.AddSingleton<IEumUserPlaylistViewModelManager, EumPlaylistViewModelManager>();

            var db = new LiteDatabase(Path.Combine(dataDir, "data.db"));

            serviceCollection.AddSingleton<ILiteDatabase>(db);
            serviceCollection.AddTransient<ITrackAggregator, TrackAggregator>();
            serviceCollection.AddSingleton<IEumUserManager, EumUserManager>();
            serviceCollection.AddSingleton<IEumUserViewModelManager, EumUserViewModelManager>();
            //serviceCollection.AddTransient<IUsersService, UsersService>();
            serviceCollection.AddTransient<IAvailableServicesProvider, BetaAvailableServicesProvider>();
            serviceCollection.AddSingleton<ICommonDirectoriesProvider>(new CommonDirectoriesProvider(dataDir)); 

            serviceCollection.AddSpotify(new SpotifyConfig
            {
                AudioQuality = AudioQuality.HIGH, 
                DeviceName = "Eum WinUI",
                DeviceType = DeviceType.Computer,
                AutoplayEnabled = true,
                CrossfadeDuration = 15000,
                LogPath = Path.Combine(dataDir, "Logs.txt"),
                CachePath = dataDir,
                TimeSyncMethod = TimeSyncMethod.NTP
            }, new NAudioPlayer());

            Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
            this.InitializeComponent();
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

    public class BetaAvailableServicesProvider : IAvailableServicesProvider
    {
        public ServiceType[] AvailableServices => new[]
        {
            ServiceType.Spotify
        };
    }
}
