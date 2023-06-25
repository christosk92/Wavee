using System.Reactive.Subjects;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Wavee.Id;
using Wavee.UI.Contracts;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.ViewModel.Setup;
public sealed class IdentityViewModel : ObservableObject, IWizardViewModel, IDisposable
{
    private readonly Subject<bool> _canGoNextObservable;
    private bool _isSignedIn;
    private bool _isBusy;
    private string _username;
    private string _password;
    private bool _canGoNext;
    private string? _errorMessage;
    private readonly Func<ServiceType, IMusicEnvironment> _environmentFactory;
    public IdentityViewModel(Func<ServiceType, IMusicEnvironment> environmentFactory)
    {
        _environmentFactory = environmentFactory;
        _canGoNextObservable = new Subject<bool>();
        _canGoNextObservable.Subscribe(x =>
        {
            CanGoNextVal = x;
        });
    }

    public bool CanGoNextVal
    {
        get => _canGoNext;
        set => SetProperty(ref _canGoNext, value);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                _canGoNextObservable.OnNext(!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password));
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                _canGoNextObservable.OnNext(!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(SecondaryActionCanInvokeOverride));
            }
        }
    }

    public string Title => "Connect to Spotify";
    public string? SecondaryActionTitle => "Skip for now";
    public IObservable<bool> CanGoNext => _canGoNextObservable;
    public double Index => 1;

    //TODO: Local only
    public bool SecondaryActionCanInvokeOverride => false;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public async Task<bool> Submit(int action)
    {
        if (action == 1)
        {
            var environment = _environmentFactory(ServiceType.Local);
            return true;
        }
        ErrorMessage = string.Empty;
        try
        {
            IsBusy = true;
            var environment = _environmentFactory(ServiceType.Spotify);

            var result = await environment.AuthService.Authenticate(
                username: _username,
                password: _password,
                CancellationToken.None);
            SettingEverythingUpViewModel.User = result;
            if (result is not null)
            {
                return true;
            }

            return false;
        }
        catch (MusicAuthenticationException authenticationException)
        {
            ErrorMessage = authenticationException.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        _canGoNextObservable.Dispose();
    }
    internal async Task<Option<UserViewModel>> SignInAsync(string username)
    {
        //split on dot
        var parts = username.Split('.');
        var type = (ServiceType)int.Parse(parts[0]);
        var id = parts[1];
        var environment = _environmentFactory(type);
        IsBusy = true;
        try
        {
            var result = await environment.AuthService.AuthenticateStored(id, CancellationToken.None);
            if (result is not null)
            {
                return result;
            }

            return Option<UserViewModel>.None;
        }
        catch (MusicAuthenticationException authenticationException)
        {
            ErrorMessage = authenticationException.Message;
            return Option<UserViewModel>.None;
        }
        finally
        {
            IsBusy = false;
        }
    }
}