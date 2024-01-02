using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Metadata;
using Wavee.Spotify.Core.Models.Track;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Interfaces.Clients;

public interface ISpotifyMetadataClient
{
    ValueTask<ISpotifyItem> GetItem(SpotifyId id, bool allowCache, CancellationToken cancellationToken = default);
    ValueTask<SpotifySimpleTrack> GetTrack(SpotifyId id, CancellationToken cancellationToken = default);
}