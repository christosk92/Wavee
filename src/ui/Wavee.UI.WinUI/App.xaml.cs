using Microsoft.UI.Xaml;
using Windows.Storage;
using LanguageExt;
using Serilog;
using Wavee.UI.Client.Playlist.Models;

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        const string vaultResource = "Wavee.UI.WinUI";
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Debug()
                .MinimumLevel.Verbose()
                .CreateLogger();

            AppProviders.GetPersistentStoragePath = () => ApplicationData.Current.LocalFolder.Path;
            AppProviders.SecurePasswordInVault = (string key, string value) =>
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                vault.Add(new Windows.Security.Credentials.PasswordCredential(vaultResource, key, value));
            };
            AppProviders.GetCredentialsFor = (string key) =>
            {
                try
                {
                    var vault = new Windows.Security.Credentials.PasswordVault();
                    var creds = vault.FindAllByResource(vaultResource);
                    foreach (var cred in creds)
                    {
                        if (cred.UserName == key)
                        {
                            cred.RetrievePassword();
                            return cred.Password;
                        }
                    }

                    return Option<string>.None;
                }
                catch
                {
                    return Option<string>.None;
                }
            };
            this.InitializeComponent();
            this.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
        }

        public static WaveeWindow MainWindow { get; private set; }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new WaveeWindow();
            MainWindow.Activate();
        }

    }
}
