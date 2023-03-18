using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;

namespace Wavee.UI.ViewModels.Login.Impl;

public sealed partial class CreateLocalProfileViewModel : AbsLoginServiceViewModel
{
    private readonly IProfileManager _profileManager;

    [ObservableProperty]
    private string? _profileName;
    [ObservableProperty]
    private string? _profilePicture;

    public CreateLocalProfileViewModel(IProfileManager profileManager)
    {
        _profileManager = profileManager;
    }


    protected override async Task SignIn(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ProfileName))
        {
            FatalLoginError = "Profile name cannot empty";
            return;
        }

        try
        {
            var profile = await _profileManager.CreateLocalProfile(ProfileName, ProfilePicture);
            OnSignedIn?.Invoke(profile);
            OnSignedIn = null;
        }
        catch (Exception x)
        {
            FatalLoginError = x.Message;
        }

        return;
    }

    [RelayCommand]
    public void GoSpotify()
    {
        OnDifferentServiceRequested?.Invoke(ServiceType.Spotify);
        OnDifferentServiceRequested = null;
    }

    [RelayCommand]
    public void GoList()
    {
        OnDifferentServiceRequested?.Invoke(null);
        OnDifferentServiceRequested = null;
    }
}