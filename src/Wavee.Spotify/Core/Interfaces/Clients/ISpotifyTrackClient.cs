using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Interfaces.Clients;


public interface ISpotifyTrackClient
{
    ValueTask<SpotifyTrack> GetTrackAsync(string trackId, bool allowCache = false, CancellationToken cancellationToken = default);
}