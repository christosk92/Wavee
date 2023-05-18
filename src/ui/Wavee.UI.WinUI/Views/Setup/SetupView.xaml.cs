using System;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Org.BouncyCastle.Asn1.Cmp;
using Wavee.Spotify.Infrastructure.Authentication;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Windows.Security.Credentials;

namespace Wavee.UI.WinUI.Views.Setup
{
    public sealed partial class SetupView : UserControl
    {
        private readonly WaveeUIRuntime _waveeUiRuntime;
        public SetupView(WaveeUIRuntime waveeUiRuntime)
        {
            _waveeUiRuntime = waveeUiRuntime;
            this.InitializeComponent();
        }

        private async void OnIconLoaded(object sender, RoutedEventArgs e)
        {
            var player = (AnimatedVisualPlayer)sender;
            await player.PlayAsync(0, 1, true);
        }

        private async void Authenticate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SigninInBorder.Visibility = Visibility.Visible;
            ErrorMessage.Text = string.Empty;
            ErrorMessage.Visibility = Visibility.Collapsed;
            var username = Username.Text;
            var password = Pwd.Password;

            var credentials = new LoginCredentials
            {
                Username = username,
                AuthData = ByteString.CopyFromUtf8(password),
                Typ = AuthenticationType.AuthenticationUserPass
            };

            var task = Spotify<WaveeUIRuntime>.Authenticate(credentials).Run(runtime: App.Runtime);
            SigninInBorder.Child = new SigninInView(task, false);
            var result = await task;
            if (result.IsFail)
            {
                var err = result.Match(Succ: _ => throw new Exception("Should not happen"), Fail: x => x);
                var ex = err.ToException();
                ErrorMessage.Visibility = Visibility.Visible;
                switch (ex)
                {
                    case SpotifyAuthenticationException authenticationException:
                        ErrorMessage.Text = authenticationException.AuthFailure.ErrorCode switch
                        {
                            ErrorCode.BadCredentials => "Check username and/or password.",
                            ErrorCode.TravelRestriction => "Reached travel limit.",
                            ErrorCode.PremiumAccountRequired => "Premium account required.",
                            ErrorCode.TryAnotherAp => "Try another AP (Try again).",
                            _ => $"Unknown error ({authenticationException.AuthFailure.ErrorCode.ToString()}"
                        };
                        break;
                    default:
                        ErrorMessage.Text = ex.Message;
                        break;
                }
            }
        }

        private void Pwd_OnPasswordChanging(PasswordBox sender, PasswordBoxPasswordChangingEventArgs args)
        {
            CanExecute();
        }

        private void Username_OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            CanExecute();
        }

        private void CanExecute()
        {
            if (!string.IsNullOrEmpty(Username.Text) && !string.IsNullOrEmpty(Pwd.Password))
            {
                AuthBtn.IsEnabled = true;
            }
            else
            {
                AuthBtn.IsEnabled = false;
            }
        }
    }
}
