using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;

namespace Wavee.UI.ViewModels.Login.Impl;

public sealed partial class SpotifyLoginViewModel : AbsLoginServiceViewModel
{
    [ObservableProperty] private string? _username;

    [ObservableProperty] private string? _password;
    // private object _spotifyService;

    protected override async Task SignIn(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            FatalLoginError = "Username and password cannot empty";
            return;
        }

        try
        {
            throw new NotSupportedException();
            //var loginTask = await _spotifyService.Login(Username, Password, ct);
            IsSigningIn = true;
            OnSignedIn?.Invoke(null);
            OnSignedIn = null;
        }
        catch (Exception x)
        {
            FatalLoginError = x.Message;
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    [RelayCommand]
    public void GoOffline()
    {
        FatalLoginError = string.Empty;
        OnDifferentServiceRequested?.Invoke(ServiceType.Local);
        OnDifferentServiceRequested = null;
    }

    [RelayCommand]
    public void GoList()
    {
        FatalLoginError = string.Empty;
        OnDifferentServiceRequested?.Invoke(null);
        OnDifferentServiceRequested = null;
    }
}