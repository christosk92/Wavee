using Wavee.Core.Enums;
using Wavee.Core.Id;

namespace Wavee.Spotify.Infrastructure.Playback;

public interface ISpotifyPlaybackClient
{
    Task PlayContext(
        string contextUri,
        int indexInContext,
        TimeSpan position,
        bool startPlaying,
        Option<PreferredQualityType> preferredQualityTypeOverride,
        CancellationToken ct = default);

    Task PlayTrack(AudioId id, CancellationToken ct = default);
}