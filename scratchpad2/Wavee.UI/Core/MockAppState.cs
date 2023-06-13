using LanguageExt;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Home;
using Wavee.UI.Core.Sys;
using Wavee.UI.Core.Sys.Mock;

namespace Wavee.UI.Core;

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
}

public record UserProfile(string Id, string Name, Option<string> ImageUrl);