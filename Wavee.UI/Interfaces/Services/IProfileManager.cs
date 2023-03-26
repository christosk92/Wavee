using Wavee.Enums;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.Interfaces.Services;

public interface IProfileManager
{
    /// <summary>
    /// Creates a new local profile with the specified name. This will create a new folder in the local profile directory.
    /// and create a new profile.json file.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <param name="profilePicture"></param>
    ValueTask<Profile> CreateLocalProfile(string profileName, string? profilePicture);

    bool HasDefaultProfile();
    bool HasAnyProfile();
    Profile? GetDefaultProfile();
    IEnumerable<Profile> GetProfiles(ServiceType serviceType);
    Task SaveProfile(Profile forProfile);
}