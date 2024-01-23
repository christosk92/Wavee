using Eum.Spotify.context;

namespace Wavee.Spfy.Playback.Contexts;

internal sealed class SpotifyNormalFiniteContext : SpotifyPagedContext
{
    public SpotifyNormalFiniteContext(Guid connectionId,
        Context context,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> createSpotifyStream)
        : base(
            connectionId: connectionId, context: context, createSpotifyStream: createSpotifyStream)
    {
        
    }
}