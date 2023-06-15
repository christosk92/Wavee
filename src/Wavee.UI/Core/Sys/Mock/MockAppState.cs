using LanguageExt;
using Wavee.UI.Core.Contracts.Album;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Home;
using Wavee.UI.Core.Contracts.Metadata;
using Wavee.UI.Core.Contracts.Playback;
using Wavee.UI.Core.Sys;

namespace Wavee.UI.Core.Sys.Mock;

internal class MockAppState : IAppState
{
    public MockAppState(UserProfile profile)
    {
        UserSettings = new UserSettings(profile.Id);
        UserProfile = profile;
    }
    public UserProfile UserProfile { get; }
    public UserSettings UserSettings { get; }
    public IHomeView Home => new MockHomeView();
    public IArtistView Artist => new MockArtistView();
    public IAlbumView Album { get; set; }
    public IRemotePlaybackClient Remote { get; }
    public IMetadataClient Metadata { get; }
    public string DeviceId { get; }
}

public record UserProfile(string Id, string Name, Option<string> ImageUrl);