using Spotify.Metadata;
using Wavee.Id;

namespace Wavee.Metadata;

public interface ISpotifyMetadataClient
{
    Task<Track> GetTrack(SpotifyId id, CancellationToken cancellationToken = default);
}