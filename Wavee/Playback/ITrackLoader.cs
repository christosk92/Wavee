using Wavee.Playback.Item;

namespace Wavee.Playback;

public interface ITrackLoader
{
    Task<PlayerLoadedTrackData?> LoadTrackAsync(string trackId, double positionMs);
}