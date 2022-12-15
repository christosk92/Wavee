using System;
using System.IO;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Enums;
using Eum.Connections.Spotify.UWPMediaPlayer;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.UI.Services;
using Eum.UI.Services.Artists;
using Eum.UI.Services.Directories;
using Eum.UI.Services.Playlists;
using Eum.UI.Services.Tracks;
using Eum.UI.Services.Users;
using Eum.UI.Spotify;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.ViewModels;

namespace Eum.UWP
{
    public sealed partial class App : Application
    {
        public App()
        {
            var dataDir = ApplicationData.Current.LocalFolder.Path;
            var logpath = Path.Combine(dataDir, "Logs.log");
            S_Log.Instance.InitializeDefaults(logpath, null);

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<MainViewModel>();

            //  serviceCollection.AddSingleton<IIdentityService, EumIdentityService>();
            serviceCollection.AddSingleton<IEumPlaylistManager, EumPlaylistManager>();
            serviceCollection.AddSingleton<IEumUserPlaylistViewModelManager, EumPlaylistViewModelManager>();

            var db = new LiteDatabase(Path.Combine(dataDir, "data.db"));

            serviceCollection.AddSingleton<ILiteDatabase>(db);
            serviceCollection.AddSingleton<ITrackAggregator, TrackAggregator>();
            serviceCollection.AddSingleton<IEumUserManager, EumUserManager>();
            serviceCollection.AddSingleton<IEumUserViewModelManager, EumUserViewModelManager>();
            serviceCollection.AddTransient<IArtistProvider, ArtistProvider>();
            //serviceCollection.AddTransient<IUsersService, UsersService>();
            serviceCollection.AddTransient<IAvailableServicesProvider, BetaAvailableServicesProvider>();
            serviceCollection.AddSingleton<ICommonDirectoriesProvider>(new CommonDirectoriesProvider(dataDir));

            serviceCollection.AddSpotify(new SpotifyConfig
            {
                AudioQuality = AudioQuality.HIGH,
                DeviceName = "Eum Uwp",
                DeviceType = DeviceType.Computer,
                AutoplayEnabled = true,
                CrossfadeDuration = 10000,
                LogPath = logpath,
                CachePath = dataDir,
                TimeSyncMethod = TimeSyncMethod.MELODY
            }, new UwpMediaPlayer());

            Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
            InitializeComponent();

            // TODO: Add your app in the app center and set your secret here. More at https://docs.microsoft.com/appcenter/sdk/getting-started/uwp
            AppCenter.Start("01e36b1a-a050-4dea-a7e6-7613e13ff548", typeof(Analytics), typeof(Crashes));
            UnhandledException += OnAppUnhandledException;

        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                Window.Current.Content = new WindowContentWrapper();
                Window.Current.Activate();
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            
        }

        private void OnAppUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/uwp/api/windows.ui.xaml.application.unhandledexception
        }
    }
    public class BetaAvailableServicesProvider : IAvailableServicesProvider
    {
        public ServiceType[] AvailableServices => new[]
        {
            ServiceType.Spotify
        };
    }
}
