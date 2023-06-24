using Microsoft.UI.Xaml;
using Wavee.UI.WinUI.Providers;
using Windows.Storage;
using LanguageExt;

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
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new WaveeWindow();
            m_window.Activate();
        }

        private Window m_window;
    }
}
