using Eum.Spotify.context;

namespace Wavee.Spfy.Playback.Contexts;

internal sealed class SpotifyStationContext : SpotifyPagedContext
{
    public SpotifyStationContext(Guid connectionId,
        Context context,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> createSpotifyStream)
        : base(connectionId, context, createSpotifyStream)
    {
    }
}