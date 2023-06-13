using System;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Wavee.UI.Core;
using LanguageExt;
using System.Linq;
using Wavee.UI.Core.Logging;
using Wavee.UI.Helpers;

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const string c_appName = "Wavee3";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            Global.RetrieveDefaultUsername = RetrieveDefaultUsername;
            Global.RetrievePasswordFromVaultForUserFunc = RetrievePasswordFromVaultForUser;
            Global.SavePasswordToVaultForUserAction = SavePasswordToVaultForUser;
            Global.GetPersistentStoragePath = () =>
            {

                var roaming = Environment.GetEnvironmentVariable("APPDATA");
                var globalPath = System.IO.Path.Combine(roaming, "Wavee3");
                return globalPath;
            };


            var roaming = Environment.GetEnvironmentVariable("APPDATA");
            var globalPath = System.IO.Path.Combine(roaming, "Wavee3");
            //var globalPath = ApplicationData.Current.LocalFolder.Path;
            var logPath = System.IO.Path.Combine(globalPath, "logs.txt");
            Logger.InitializeDefaults(logPath, null);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        public static MainWindow MainWindow { get; private set; }


        private static Option<string> RetrieveDefaultUsername()
        {
            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                var credential = vault.RetrieveAll().FirstOrDefault(c => c.Resource is c_appName);
                if (credential == null)
                {
                    return Option<string>.None;
                }
                return credential.UserName;
            }
            catch
            {
                return Option<string>.None;
            }
        }
        private static void SavePasswordToVaultForUser(string username, string password)
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Add(new Windows.Security.Credentials.PasswordCredential(c_appName, username, password));
        }

        private static Option<string> RetrievePasswordFromVaultForUser(string username)
        {
            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                var credential = vault.FindAllByResource(c_appName).FirstOrDefault(x => x.UserName == username);
                if (credential == null)
                {
                    return Option<string>.None;
                }

                credential.RetrievePassword();
                return Option<string>.Some(credential.Password);
            }
            catch
            {
                return Option<string>.None;
            }
        }

    }
}
