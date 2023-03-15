using Microsoft.UI.Xaml;
using System;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Wavee.Player.NAudio;
using Wavee.Spotify;
using Wavee.Spotify.ConnectState;
using Wavee.Spotify.Player;
using Wavee.Spotify.Session;
using Wavee.UI.AudioImport;
using Wavee.UI.Identity.Users;
using Wavee.UI.Utils;
using Wavee.UI.Utils.Extensions;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.ViewModels.Identity;
using Wavee.UI.ViewModels.Identity.User;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.WinUI.Player;
using Wavee.UI.WinUI.Utils;
using WinUIEx;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI
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
            this.InitializeComponent();
            var services = ConfigureServices();
            Ioc.Default.ConfigureServices(services);

            //get UVM (to register events)
            _ = services.GetRequiredService<UserManagerViewModel>();
            _ = services.GetRequiredService<LocalAudioManagerViewModel>();

        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MWindow = new MainWindow();
            MWindow.Activate();
        }

        public static WindowEx MWindow;


        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services
        {
            get;
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            string workDir = string.Empty;
            try
            {
                workDir = ApplicationData.Current.LocalFolder.Path;
            }
            catch (Exception x)
            {
                workDir = EnvironmentHelpers.GetDataDir("Wavee");
            }

            var services = new ServiceCollection();
            var serilog = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(Path.Combine(workDir, "waveeui.log"), rollingInterval: RollingInterval.Day)
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();
            services.AddLogging(builder =>
            {
                builder.AddSerilog(serilog);
            });


            services.AddSingleton<ISpotifySession, SpotifySession>();
            services.AddSingleton<ISpotifyConnectState, SpotifyConnectState>();
            services.AddSingleton<ISpotifyPlayer, SpotifyPlayer>();

            services.AddSingleton(sp =>
            {
                var umv = new UserManagerViewModel(sp.GetRequiredService<WaveeUserManagerFactory>());
                return umv;
            });

            services.AddScoped<ShellViewModel>();
            services.AddTransient<SignInViewModel>();

            services.AddTransient<WaveeUserManagerFactory>();

            services.AddSpotify(workDir)
                .AddLocal(workDir)
                .AddSingleton<ILocalFilePlayer, WinUIMediaPlayer>();

            services.AddTransient<ArtistRootViewModel>();

            services.AddSingleton<IAudioSink, NAudioSink>();

            services.AddSingleton<IUiDispatcher>(new WinUIDispatcher());
            return services.BuildServiceProvider();
        }

    }

    internal class WinUIDispatcher : IUiDispatcher
    {
        public bool Dispatch(DispatcherQueuePriority priority, Action callback)
        {
            return App.MWindow.DispatcherQueue.TryEnqueue(priority switch
            {
                DispatcherQueuePriority.Low => Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                DispatcherQueuePriority.Normal => Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                DispatcherQueuePriority.High => Microsoft.UI.Dispatching.DispatcherQueuePriority.High,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            }, () => callback());
        }
    }
}
