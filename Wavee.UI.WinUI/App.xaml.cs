using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using ReactiveUI;
using Splat;
using Wavee.UI.Helpers.Extensions;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Playback.Player;
using Wavee.UI.Services.Db;
using Wavee.UI.Services.Import;
using Wavee.UI.Services.Profiles;
using Wavee.UI.ViewModels.Home;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Login;
using Wavee.UI.WinUI.Interfaces.Services;
using Wavee.UI.WinUI.Playback;
using Wavee.UI.WinUI.Services;

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
            Locator.CurrentMutable.InitializeReactiveUI();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var rm = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            var rc = rm.MainResourceMap;
            var container = CreateServices();
            Ioc.Default.ConfigureServices(container
                .AddSingleton<IStringLocalizer>(new WinUIResourcesStringLocalizer(rc))
                .BuildServiceProvider());

            m_window = new MainWindow();
            MainWindow = m_window as MainWindow;
            m_window.Activate();
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddTransient<LoginViewModel>();

            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IAppDataProvider, AppDataProvider>();

            services.AddSingleton<IStringLocalizer, WinUIResourcesStringLocalizer>();

            services.AddSingleton<IProfileManager, ProfileManager>();

            services.AddLocal();

            services.AddSingleton<ILocalFilePlayer, WinUIMediaPlayer>();

            services.AddTransient<LibraryRootViewModel>();
            services.AddScoped<LibrarySongsViewModel>();
            services.AddScoped<LibraryAlbumsViewModel>();
            services.AddScoped<LibraryArtistsViewModel>();

            services.AddScoped<IPlaycountService, PlaycountService>();

            return services;
        }

        private Window m_window;

        public static MainWindow MainWindow { get; private set; }
    }
}