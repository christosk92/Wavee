using System;
using System.Linq;
using Windows.Storage;
using Eum.Spotify.connectstate;
using LanguageExt;
using Microsoft.UI.Xaml;
using Wavee.Spotify;
using ReactiveUI;

namespace Wavee.UI.WinUI
{
    public partial class App : Application
    {
        private const string c_appName = "Wavee2";
        public App()
        {
            this.InitializeComponent();
            var a = RxApp.MainThreadScheduler;

            //TODO: read from config file
            SpotifyConfig = new SpotifyConfig(
                remote: new SpotifyRemoteConfig(
                    deviceName: "Wavee",
                    deviceType: DeviceType.Computer
                ),
                playback: new SpotifyPlaybackConfig(
                    crossfadeDuration: TimeSpan.FromSeconds(10),
                    preferedQuality: PreferredQualityType.High
                ),
                cache: new SpotifyCacheConfig(
                    cacheRoot: ApplicationData.Current.LocalCacheFolder.Path
                )
            );
            PlatformSpecificServices.RetrieveDefaultUsername = RetrieveDefaultUsername;
            PlatformSpecificServices.RetrievePasswordFromVaultForUserFunc = RetrievePasswordFromVaultForUser;
            PlatformSpecificServices.SavePasswordToVaultForUserAction = SavePasswordToVaultForUser;
            PlatformSpecificServices.GetPersistentStoragePath = () => ApplicationData.Current.LocalFolder.Path;
        }


        public static SpotifyConfig SpotifyConfig { get; private set; }

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

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private Window m_window;
    }
}
