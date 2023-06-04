using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.Spotify;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Shell;

namespace Wavee.UI.WinUI.Views.SignIn;

public sealed partial class EnterCredentialsView : UserControl
{
    public EnterCredentialsView()
    {
        ViewModel = new EnterCredentialsViewModel();
        this.InitializeComponent();
        var dispatcherQueue = this.DispatcherQueue;
        SignInCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var config = App.SpotifyConfig;
            var credentials = new LoginCredentials
            {
                AuthData = ByteString.CopyFromUtf8(PasswordBox.Password),
                Username = UsernameTextBox.Text,
                Typ = AuthenticationType.AuthenticationUserPass
            };

            var tcs = new TaskCompletionSource<Unit>();
            //replace with signinin view
            MainWindow.Instance.Content = new SigninInView(
                credentials,
                onError: e =>
                {
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                    {
                        tcs.SetResult(Unit.Default);
                        ViewModel.ErrorMessage = e.Message;

                        MainWindow.Instance.Content = this;
                    });
                },
                onDone: state =>
                {
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                    {
                        tcs.SetResult(Unit.Default);
                        MainWindow.Instance.Content = new ShellView(state.User);
                    });
                },
                config);
            await tcs.Task;
        });
    }

    public EnterCredentialsViewModel ViewModel { get; }
    public ICommand SignInCommand { get; }

    public Visibility HasErrorMessage(string? message)
    {
        //if null, -> false
        //if empty, -> false
        return string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}