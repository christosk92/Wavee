using System.Text.Json;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.Services.Profiles
{
    public sealed class ProfileManager : IProfileManager
    {
        private readonly IFileService _fileService;
        private readonly HashSet<Profile> _profiles;

        public ProfileManager(IFileService fileService)
        {
            _fileService = fileService;
            _profiles = new HashSet<Profile>(fileService.EnumerateIn(nameof(ServiceType.Local))
                .Concat(fileService.EnumerateIn(nameof(ServiceType.Spotify)))
                .Select(j =>
                {
                    using var fs = File.OpenRead(j.FullName);
                    return JsonSerializer.Deserialize<Profile>(fs);
                })!);
        }

        public async ValueTask<Profile> CreateLocalProfile(string profileName, string? profilePicture)
        {
            var guid = Guid.NewGuid().ToString();

            var profile = new Profile(
                Id: guid,
                ServiceType: ServiceType.Local,
                DisplayName: profileName,
                Image: profilePicture,
                Properties: new Dictionary<string, string>(),
                IsDefault: !HasAnyProfile(),
                SidebarWidth: 200,
                SidebarExpanded: true,
                LargeImage: false,
                SavedTracks: new HashSet<string>());

            await _fileService.Write(profile,
                Path.Combine(nameof(ServiceType.Local), "profiles", $"{profile.Id}.json"));

            _profiles.Add(profile);
            return profile;
        }

        public bool HasDefaultProfile() => _profiles.Any(j => j.IsDefault);
        public bool HasAnyProfile() => _profiles.Count > 0;

        public Profile? GetDefaultProfile() => _profiles
            .FirstOrDefault(a => a.IsDefault);

        public IEnumerable<Profile> GetProfiles(ServiceType serviceType) => _profiles
            .Where(a => a.ServiceType == serviceType);

        public ValueTask SaveProfile(Profile forProfile)
        {
            _profiles.RemoveWhere(a => a.Id == forProfile.Id);
            _profiles.Add(forProfile);
            return _fileService.Write(forProfile,
                Path.Combine(nameof(ServiceType.Local), "profiles", $"{forProfile.Id}.json"));
        }
    }
}