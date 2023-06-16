using Wavee.UI.Core.Contracts.Album;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Home;
using Wavee.UI.Core.Contracts.Library;
using Wavee.UI.Core.Contracts.Metadata;
using Wavee.UI.Core.Contracts.Playback;
using Wavee.UI.Core.Contracts.Search;
using Wavee.UI.Core.Sys;
using Wavee.UI.Core.Sys.Mock;

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
    IAlbumView Album { get; }
    IRemotePlaybackClient Remote { get; }
    IMetadataClient Metadata { get; }
    ILibraryView Library { get; }
    ISearchClient Search { get; }

    string DeviceId { get; }
}