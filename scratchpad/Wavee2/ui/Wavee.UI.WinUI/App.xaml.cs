using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;
using Wavee.Helpers;
using Wavee.Helpers.Logging;
using Wavee.UI.Config;
using Wavee.UI.States;
using Wavee.UI.WinUI.Views;

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                Logger.LogError(ex);

                RxApp.MainThreadScheduler.Schedule(() => throw new ApplicationException("Exception has been thrown in unobserved ThrownExceptions", ex));
            });
            var dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Wavee", "UI"));
            _ = new AppState(new AppConfig(dataDir));
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new Window
            {
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop(),
                Content = MainContent
            };
            MainWindow.Activate();
        }
        public static MainContentView MainContent { get; } = new MainContentView();
        public static Window MainWindow { get; private set; }
    }
}
