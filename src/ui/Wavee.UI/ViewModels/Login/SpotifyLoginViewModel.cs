using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Providers;
using Wavee.UI.Providers.Spotify;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Login;

public sealed class SpotifyLoginViewModel : ObservableObject
{
    private string? _navigateTo;
    private bool _isBusy;
    private readonly IDispatcher _dispatcher;
    private readonly WaveeUIAuthenticationModule _authenticationModule;
    public SpotifyLoginViewModel(IDispatcher dispatcher, WaveeUIAuthenticationModule authenticationModule)
    {
        _dispatcher = dispatcher;
        _authenticationModule = authenticationModule;
        OnNavigated = new AsyncRelayCommand<string>(OnNavigatedFunc);

        Prepare();
    }

    public string? NavigateTo
    {
        get => _navigateTo;
        set => this.SetProperty(ref _navigateTo, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.SetProperty(ref _isBusy, value);
    }

    public IRelayCommand<string> OnNavigated { get; }

    private void Prepare()
    {
        if (_authenticationModule.IsOAuth)
        {
            NavigateTo = _authenticationModule.OAuthUrl;
        }
        else if (_authenticationModule.AuthenticationTask is not null)
        {
            var busyTask = _authenticationModule.AuthenticationTask;
            IsBusy = !busyTask.IsCompleted;
            Task.Run(async () =>
            {
                try
                {
                    await busyTask;
                }
                catch (Exception x)
                {
                    Debugger.Break();
                }
                finally
                {
                    _dispatcher.Dispatch(() => IsBusy = false);
                }
            });
        }
        else if (_authenticationModule.UserNamePasswordAuthentication is not null)
        {
            // Not supported anymore
        }
    }
    private async Task OnNavigatedFunc(string? arg)
    {
        if (string.IsNullOrEmpty(arg)) return;

        if (_authenticationModule.OAuthCallback is not null)
        {
            var callbackResult = await _authenticationModule.OAuthCallback(arg);
            if (!callbackResult)
            {

            }
        }
    }
}