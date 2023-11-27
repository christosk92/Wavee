using Spotify.Metadata;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Infrastructure.Persistent;

internal sealed class SpotifyTrackRepository : ISpotifyTrackRepository
{
    public void AddTrack(Track track)
    {
        
    }

    public bool TryGetTrack(SpotifyId id, out Track o)
    {
        o = null;
        return false;
    }
}

public interface ISpotifyTrackRepository
{
    void AddTrack(Track track);
    bool TryGetTrack(SpotifyId id, out Track o);
}