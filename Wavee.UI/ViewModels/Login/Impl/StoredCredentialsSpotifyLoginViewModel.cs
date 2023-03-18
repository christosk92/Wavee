using Wavee.UI.Models.Profiles;

namespace Wavee.UI.ViewModels.Login.Impl;

public sealed class StoredCredentialsSpotifyLoginViewModel : AbsLoginServiceViewModel
{
    private readonly Profile _profile;

    public StoredCredentialsSpotifyLoginViewModel(Profile profile)
    {
        _profile = profile;
        IsSigningIn = true;
        SignIn();
    }

    protected sealed override Task SignIn(CancellationToken ct = default)
    {
        //Get reusing credentials from profile
        //Login with credentials
        //update profile with new credentials
        //OnSignedIn?.Invoke();
        //OnSignedIn = null;
        throw new NotImplementedException();
    }
}