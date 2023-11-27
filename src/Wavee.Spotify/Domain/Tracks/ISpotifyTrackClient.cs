using Spotify.Metadata;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Tracks;

public interface ISpotifyTrackClient
{
    ValueTask<Track> GetTrack(SpotifyId trackId, CancellationToken cancellationToken = default);
}