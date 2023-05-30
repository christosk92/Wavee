using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.Helpers.Logging;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Authentication;
using Wavee.UI.States;
using Wavee.UI.States.Spotify;
using Wavee.UI.States.Spotify.Models.Response;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views;

public sealed partial class SignInView : UserControl, INotifyPropertyChanged
{
    private bool _isSigningIn;
    private string? _errorMessage;
    private readonly IObservable<bool> _canExecuteSignInCommand;
    private Action<Option<User>> _onSignInAction;
    private Option<AuthenticationType> _authenticationType;
    public SignInView(Action<Option<User>> onSignInAction)
    {
        _onSignInAction = onSignInAction;
        this.InitializeComponent();

        //can execute: username and password are not empty
        _canExecuteSignInCommand = this.WhenAnyValue(
            x => x.UsernameTextBox.Text,
            x => x.PasswordBox.Password,
                       (username, password) => !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            .ObserveOn(RxApp.MainThreadScheduler);

        SignInCommand = ReactiveCommand.CreateFromTask(SignInTask, _canExecuteSignInCommand);

        Task.Run(() =>
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var data = vault.RetrieveAll();
                if (data.Any(x => x.Resource is "Wavee2"))
                {
                    var credential = data.First(x => x.Resource is "Wavee2");
                    credential.RetrievePassword();
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        _authenticationType = AuthenticationType.AuthenticationStoredSpotifyCredentials;
                        this.UsernameTextBox.Text = credential.UserName;
                        this.PasswordBox.Password = vault.Retrieve(credential.Resource, credential.UserName).Password;
                        SignInCommand.Execute(null);
                    });
                }
            }
            catch (Exception e)
            {

            }
        });
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool IsSigningIn
    {
        get => _isSigningIn;
        set => SetField(ref _isSigningIn, value);
    }

    private async Task SignInTask(CancellationToken arg)
    {
        ErrorMessage = string.Empty;
        var username = this.UsernameTextBox.Text;
        var password = this.PasswordBox.Password;

        var credentials = new LoginCredentials
        {
            Username = username,
            AuthData = _authenticationType.Match(Some: _ => ByteString.FromBase64(password), None: () => ByteString.CopyFromUtf8(password)),
            Typ = _authenticationType.IfNone(AuthenticationType.AuthenticationUserPass)
        };

        var potentialClient = await Authenticate(AppState.Instance.SpotifyState.SpotifyConfig, credentials, arg);

        if (potentialClient.IsSome)
        {
            var client = potentialClient.ValueUnsafe();
            SpotifyState.Instance.Client = client;
            //no ct: dont cancel at this point
            var user = await SpotifyState.Instance.GetHttp<PrivateSpotifyUser>(SpotifyEndpoints.PublicApi.GetMe)
                .IfLeft(_ => throw new NotSupportedException("Invalid state"))();

            var finalUser = user.Match(
                Succ: x => new User(
                    Id: client.WelcomeMessage.CanonicalUsername,
                    IsDefault: true,
                    DisplayName: x.DisplayName,
                    ImageId: x.Images.FirstOrDefault().Url is string s ? s : Option<string>.None,
                    Metadata: HashMap<string, string>.Empty
                ),
                Fail: err =>
                {
                    Logger.LogError(err);
                    return new User(
                        Id: client.WelcomeMessage.CanonicalUsername,
                        IsDefault: true,
                        DisplayName: Option<string>.None,
                        ImageId: Option<string>.None,
                        Metadata: HashMap<string, string>.Empty
                    );
                }
            );
            _ = Task.Run(() =>
            {
                //add to vault
                var vault = new Windows.Security.Credentials.PasswordVault();
                vault.Add(new Windows.Security.Credentials.PasswordCredential(
                    "Wavee2",
                    userName: client.WelcomeMessage.CanonicalUsername,
                    password: client.WelcomeMessage.ReusableAuthCredentials.ToBase64()
                ));
            });


            _onSignInAction(Option<User>.Some(finalUser));
            _onSignInAction = null;
        }
    }

    private async Task<Option<SpotifyClient>> Authenticate(
        SpotifyConfig config,
        LoginCredentials credentials,
        CancellationToken ct = default)
    {
        IsSigningIn = true;
        try
        {
            var result = await SpotifyClient.CreateAsync(credentials, config, ct);
            return result;
        }
        catch (SpotifyAuthenticationException authenticationException)
        {
            ErrorMessage = authenticationException.AuthFailure.ErrorCode switch
            {
                ErrorCode.BadCredentials => "Check credentials.",
                _ => authenticationException.AuthFailure.ErrorCode.ToString()
            };
            return Option<SpotifyClient>.None;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            return Option<SpotifyClient>.None;
        }
        finally
        {
            IsSigningIn = false;
        }
    }


    public ICommand SignInCommand { get; }
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public Visibility HasErrorMessage(string s)
    {
        return string.IsNullOrEmpty(s)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}