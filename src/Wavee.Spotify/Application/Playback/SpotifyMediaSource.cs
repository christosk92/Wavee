using Wavee.Domain.Playback.Player;

namespace Wavee.Spotify.Application.Playback;

public sealed class SpotifyMediaSource : IWaveeMediaSource
{
    public ValueTask<Stream> CreateStream()
    {
        throw new NotImplementedException();
    }

    public TimeSpan Duration { get; }
    public TimeSpan CurrentTime { get; }
}