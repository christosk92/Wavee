using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.ViewModel.Identity;
public sealed class IdentityViewModel : ObservableObject
{
    private bool _isSignedIn;
    private bool _isBusy;
    private string _username;
    private string _password;

    public string Username
    {
        get => _username;
        set => this.SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => this.SetProperty(ref _password, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => this.SetProperty(ref _isBusy, value);
    }

    internal Task SignInAsync(string username, string password)
    {
        return Task.CompletedTask;
    }
}    