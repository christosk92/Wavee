using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.ViewModels.Login.Impl;

public partial class SelectProfileViewModel : AbsLoginServiceViewModel
{
    private readonly IProfileManager _profileManager;
    public SelectProfileViewModel(IProfileManager profileManager)
    {
        _profileManager = profileManager;
        Profiles = new[]
            {
                ServiceType.Local,
                ServiceType.Spotify
            }
            .Select(a => new GroupedProfiles(profileManager.GetProfiles(a).ToImmutableArray(), a switch
            {
                ServiceType.Local => "Offline",
                ServiceType.Spotify => "Spotify",
                _ => throw new ArgumentOutOfRangeException(nameof(a), a, null)
            }))
            .Where(a => a.Profiles.Length > 0)
            .ToImmutableArray();
    }

    public ImmutableArray<GroupedProfiles> Profiles { get; }

    public bool HasAnyProfile => Profiles.Length > 0;

    protected override Task SignIn(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void GoSpotify()
    {
        OnDifferentServiceRequested?.Invoke(ServiceType.Spotify);
    }

    [RelayCommand]
    public void GoLocal()
    {
        OnDifferentServiceRequested?.Invoke(ServiceType.Local);
    }
}