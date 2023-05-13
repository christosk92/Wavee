using Wavee.Core.Id;

namespace Wavee.Spotify.Infrastructure.Playback;

public interface ISpotifyPlaybackClient
{
    Task PlayContext(
        string contextUri,
        int indexInContext,
        TimeSpan position,
        bool startPlaying,
        CancellationToken ct = default);

    Task PlayTrack(AudioId id, CancellationToken ct = default);
}