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
using Eum.UI.Services.Albums;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage.Streams;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.UI.Helpers;
using Eum.UI.Users;
using Eum.UI.ViewModels.Settings;
using Microsoft.Toolkit.Uwp.Helpers;
using ColorHelper = Microsoft.Toolkit.Uwp.Helpers.ColorHelper;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace Eum.UWP
{
    public class UWP_ThemeSelectorServiceFactory : IThemeSelectorServiceFactory
    {
        public IThemeSelectorService GetThemeSelectorService(EumUser forUser)
        {
            return new UWPUserThemeSelectorService(forUser);
        }
    }

    [INotifyPropertyChanged]
    public partial class UWPUserThemeSelectorService : IThemeSelectorService
    {
        [NotifyPropertyChangedFor(nameof(GlazeIsCustomColor))]
        [ObservableProperty]
        private string _glaze;
        private bool _initialized = false;
        private readonly EumUser _forUser;
        public UWPUserThemeSelectorService(EumUser forUser)
        {
            _forUser = forUser;
            if (string.IsNullOrEmpty(forUser.Accent))
            {
                Glaze = ColorHelper.ToHex(Colors.Transparent);
            }
            else
            {
                SetGlaze(forUser.Accent);
            }
            SetTheme(_forUser.AppTheme);

        }
        public bool GlazeIsCustomColor => Glaze.StartsWith("#");
        private ElementTheme AsElementTheme => Theme switch
        {
            AppTheme.Dark => ElementTheme.Dark,
            AppTheme.Light => ElementTheme.Light,
            AppTheme.SystemDefault => ElementTheme.Default,
            _ => throw new ArgumentOutOfRangeException()
        };

        public AppTheme Theme { get; set; } = AppTheme.SystemDefault;
        public AppTheme ActualTheme { get; }


        public void SetTheme(AppTheme theme)
        {
            Theme = theme;

            SetRequestedTheme();
            _forUser.AppTheme = theme;
        }

        private bool _showedAccentMessageAlready = false;

        public void SetGlaze(string colorCodeHex)
        {
            switch (colorCodeHex)
            {
                case "System Color":
                    Glaze = Colors.Transparent.ToHex();
                    _forUser.Accent = "System Color";
                    break;
                case "Page Dependent":
                    Glaze = "Page Dependent";
                    _forUser.Accent = "Page Dependent";
                    break;
                case "Playback Dependent":
                    Glaze = "Playback Dependent";
                    _forUser.Accent = "Playback Dependent";
                    break;
                default:
                    if (colorCodeHex is null)
                    {
                        Glaze = Colors.Transparent.ToHex();
                    }
                    else
                    {
                        var f = ColorHelper.ToColor(colorCodeHex);
                        colorCodeHex = (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                        Glaze = colorCodeHex;
                    }

                    _forUser.LastAccent = Glaze;
                    _forUser.Accent = Glaze;
                    break;
            }
            GlazeChanged?.Invoke(this, Glaze);
        }

        public event EventHandler<string> GlazeChanged;

        public void SetRequestedTheme()
        {
            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = AsElementTheme;
            }
        }
    }

    public sealed partial class App : Application
    {
        public App()
        {
            var dataDir = ApplicationData.Current.LocalFolder.Path;
            var logpath = Path.Combine(dataDir, "Logs_UWP.log");

            S_Log.Instance.InitializeDefaults(logpath, null);
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<MainViewModel>();

            //  serviceCollection.AddSingleton<IIdentityService, EumIdentityService>();
            serviceCollection.AddSingleton<IEumPlaylistManager, EumPlaylistManager>();
            serviceCollection.AddSingleton<IEumUserPlaylistViewModelManager, EumPlaylistViewModelManager>();

            var db = new LiteDatabase(Path.Combine(dataDir, "data.db"));

            serviceCollection.AddTransient<IFileHelper, UWP_RandomAccessStream>();
            serviceCollection.AddSingleton<ILiteDatabase>(db);
            serviceCollection.AddTransient<ITrackAggregator, TrackAggregator>();
            serviceCollection.AddSingleton<IEumUserManager, EumUserManager>();
            serviceCollection.AddSingleton<IEumUserViewModelManager, EumUserViewModelManager>();
            serviceCollection.AddTransient<IArtistProvider, ArtistProvider>();
            serviceCollection.AddTransient<IAlbumProvider, AlbumProvider>();
            //serviceCollection.AddTransient<IUsersService, UsersService>();
            serviceCollection.AddTransient<IAvailableServicesProvider, BetaAvailableServicesProvider>();
            serviceCollection.AddSingleton<ICommonDirectoriesProvider>(new CommonDirectoriesProvider(dataDir));

            serviceCollection.AddTransient<IThemeSelectorServiceFactory, UWP_ThemeSelectorServiceFactory>();

            serviceCollection.AddSpotify(new SpotifyConfig
            {
                AudioQuality = AudioQuality.VERY_HIGH,
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
            UnhandledException += OnUnhandledException;

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


        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            S_Log.Instance.LogError(e.Message);
            S_Log.Instance.LogError(e.Exception);
            e.Handled = true;
        }
    }
    public class BetaAvailableServicesProvider : IAvailableServicesProvider
    {
        public ServiceType[] AvailableServices => new[]
        {
            ServiceType.Spotify
        };
    }
    public class UWP_RandomAccessStream : IFileHelper
    {
        public async ValueTask<Stream> GetStreamForString(string playlistImagePath, CancellationToken cancellationToken)
        {
            var random = RandomAccessStreamReference.CreateFromUri(new Uri(playlistImagePath));
            IRandomAccessStream stream = await Task.Run(async () => await random.OpenReadAsync(), cancellationToken);
            return stream.AsStreamForRead();
        }

    }

}
