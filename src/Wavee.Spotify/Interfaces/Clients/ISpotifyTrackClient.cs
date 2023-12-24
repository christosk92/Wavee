using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Interfaces.Clients;


public interface ISpotifyTrackClient
{
    ValueTask<SpotifyTrack> Get(SpotifyId trackId, CancellationToken cancellationToken = default);
}