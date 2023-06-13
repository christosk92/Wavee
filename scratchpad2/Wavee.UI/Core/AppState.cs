using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Home;
using Wavee.UI.Core.Sys;

namespace Wavee.UI.Core;

// internal sealed class LiveAppState : IAppState
// {
//     public UserSettings UserSettings { get; }
//     public IHomeView Home { get; }
// }

public interface IAppState
{
    UserProfile UserProfile { get; }
    UserSettings UserSettings { get; }

    IHomeView Home { get; }
    IArtistView Artist { get; }
}