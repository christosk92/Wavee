using Eum.Spotify.spircs;
using Wavee.ViewModels.Helpers;
using Wavee.ViewModels.Interfaces;

namespace Wavee.ViewModels.Models.Users;

public class User
{
    private UserState _state;

    public User(
        string dataDir,
        string id,
        IUserAuthenticator userAuthenticator)
    {
        Id = id;
        RuntimeParams.SetDataDir(dataDir);
        UserAuthenticator = userAuthenticator;
    }

    public event EventHandler<UserState>? StateChanged;

    public string Id { get; }
    public string Name { get; set; }
    public IUserAuthenticator UserAuthenticator { get; }
    public UserState State
    {
        get => _state;
        private set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            StateChanged?.Invoke(this, _state);
        }
    }
    public void Initialize()
    {
        if (State > UserState.WaitingForInit)
        {
            throw new InvalidOperationException($"{nameof(State)} must be {UserState.Uninitialized} or {UserState.WaitingForInit}. Current state: {State}.");
        }

        try
        {
            UserAuthenticator.Authenticated += UserAuthenticatorOnAuthenticated;

            State = UserState.Initialized;
        }
        catch
        {
            State = UserState.Uninitialized;
            throw;
        }
    }

    private void UserAuthenticatorOnAuthenticated(object? sender, WaveeUserInfo e)
    {
        Name = e.Name;
    }
}