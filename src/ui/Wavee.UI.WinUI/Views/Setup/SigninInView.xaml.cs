using System;
using System.Threading.Tasks;
using Eum.Spotify;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Windows.Security.Credentials;

namespace Wavee.UI.WinUI.Views.Setup
{
    public sealed partial class SigninInView : UserControl
    {
        public const string VAULT_KEY = "WaveeSpotify";


        private bool _naivgateToSetupView;
        public SigninInView(ValueTask<Fin<APWelcome>> signinTask, bool naivgateToSetupView)
        {
            SignInTask = signinTask;
            _naivgateToSetupView = naivgateToSetupView;
            this.InitializeComponent();
        }
        public ValueTask<Fin<APWelcome>> SignInTask { get; }

        private async void SigninInView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var result = await Task.Run(async() => await SignInTask);
            //if ok , navigate to main page
            //if not 
            if (result.IsFail)
            {
                if (_naivgateToSetupView)
                {
                    this.Content = new SetupView(App.Runtime);
                }
            }
            else
            {
                var welcomeMessage = result.Match(Succ: x => x, Fail: _ => throw new Exception("Should not happen"));
                var user = new User
                {
                    Id = welcomeMessage.CanonicalUsername,
                    IsDefault = true,
                    DisplayName = Option<string>.None,
                    ImageId = Option<string>.None,
                    Metadata = HashMap<string, string>.Empty
                };
                _ = await UserManagment<WaveeUIRuntime>.CreateOrOverwriteUser(user).Run(runtime: App.Runtime);

                PasswordVault vault = new();
                vault.Add(new PasswordCredential(VAULT_KEY, welcomeMessage.CanonicalUsername,
                    $"{welcomeMessage.ReusableAuthCredentials.ToBase64()}-{(int)welcomeMessage.ReusableAuthCredentialsType}"));

                App.MWindow.Content = new ShellView(App.Runtime, user);
            }
        }
    }
}
