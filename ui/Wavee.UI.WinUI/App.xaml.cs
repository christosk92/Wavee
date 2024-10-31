using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ReactiveUI;
using Wavee.ViewModels.State;
using Wavee.ViewModels;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.Infrastructure;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Windows.Storage;
using Path = System.IO.Path;
using Wavee.ViewModels.Service;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static ApplicationStateManager _applicationStateManager;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Instance = this;

            var dataDir = ApplicationData.GetDefault().LocalFolder.Path;
            UiConfig uiConfig = LoadOrCreateUiConfig(dataDir);
            var configFilePath = Path.Combine(dataDir, "Config.json");
            ViewModels.Services.Initialize(
                dataDir,
                configFilePath, new PersistentConfig(), uiConfig, new SingleInstanceChecker(),
                new TerminateService(TerminateApplicationAsync, TerminateApplication));
            var uiContext = CreateUiContext();
            UiContext.Default = uiContext;
            _applicationStateManager = new ApplicationStateManager(
                new MainWindowFactory(),
                new ActivatableApplicationLifetime(this),
                uiContext,
                false);
        }

        public static Window ActualWindow => _applicationStateManager.ActualWindow as MainWindow ?? throw new InvalidOperationException("ActualWindow is null");

        private void TerminateApplication()
        {
            throw new NotImplementedException();
        }

        private Task TerminateApplicationAsync()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<LaunchActivatedEventArgs> Activated;

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
        }

        public static App Instance { get; private set; }

        private UiContext CreateUiContext()
        {
            var applicationSettings = CreateApplicationSettings();

            // This class (App) represents the actual Wavee Application and it's sole presence means we're in the actual runtime context (as opposed to unit tests)
            // Once all ViewModels have been refactored to receive UiContext as a constructor parameter, this static singleton property can be removed.
            return new UiContext(
                applicationSettings,
                new UserRepository());
        }

        private static IApplicationSettings CreateApplicationSettings()
        {
            return new ApplicationSettings(
                ViewModels.Services.PersistentConfigFilePath,
                ViewModels.Services.PersistentConfig,
                ViewModels.Services.UiConfig);
        }

        private static UiConfig LoadOrCreateUiConfig(string dataDir)
        {
            Directory.CreateDirectory(dataDir);

            UiConfig uiConfig = new(Path.Combine(dataDir, "UiConfig.json"));
            uiConfig.LoadFile(createIfMissing: true);

            return uiConfig;
        }
    }
}