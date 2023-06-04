using System;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.WinUI.Views.Shell;
using Wavee.UI.WinUI.Views.SignIn;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Instance = this;

        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        var dispatcher = this.DispatcherQueue;
        bool didSomething = false;
        if (PlatformSpecificServices.RetrieveDefaultUsername is not null)
        {
            var username = PlatformSpecificServices.RetrieveDefaultUsername();
            if (username.IsSome && PlatformSpecificServices.RetrievePasswordFromVaultForUserFunc != null)
            {
                var password = PlatformSpecificServices.RetrievePasswordFromVaultForUserFunc(username.ValueUnsafe());
                if (password.IsSome)
                {
                    didSomething = true;

                    try
                    {
                        this.Content = new SigninInView(
                            credentials: new LoginCredentials
                            {
                                Username = username.ValueUnsafe(),
                                AuthData = ByteString.FromBase64(password.ValueUnsafe()),
                                Typ = AuthenticationType.AuthenticationStoredSpotifyCredentials
                            },
                            onError: e =>
                            {
                                dispatcher.TryEnqueue(DispatcherQueuePriority.High,
                                    () => { this.Content = new EnterCredentialsView(); });
                            },
                            onDone: state =>
                            {
                                dispatcher.TryEnqueue(DispatcherQueuePriority.High,
                                    () => { this.Content = new ShellView(state.User); });
                            },
                            config: App.SpotifyConfig
                        );
                    }
                    catch (Exception)
                    {
                        this.Content = new EnterCredentialsView();
                    }
                }
            }
        }

        if (!didSomething)
        {
            this.Content = new EnterCredentialsView();
        }
    }

    public static MainWindow Instance { get; private set; } = null!;
}