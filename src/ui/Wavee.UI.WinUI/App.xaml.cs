using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Wavee.UI.WinUI.Debug.Debug;
using Wavee.UI.WinUI.Views.Home;

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
            Ioc.Default.ConfigureServices(BuildViews().BuildServiceProvider());
        }

        public static IServiceCollection BuildViews()
        {
            var services = new ServiceCollection();
            services.AddTransient<HomeView>();
            return services;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

#if DEBUG
            var debugWindow = WindowHelper.CreateWindow();
            debugWindow.Content = new DebugView();
            debugWindow.Activate();
#endif
        }

        private Window m_window;
    }
}
