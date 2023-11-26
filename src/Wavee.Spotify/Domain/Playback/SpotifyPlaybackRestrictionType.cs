namespace Wavee.Spotify.Domain.Playback;

public enum SpotifyPlaybackRestrictionType
{
    Pause,
    Resume,
    Seek,
    SkipNext,
    SkipPrev,
    RepeatTrack,
    RepeatContext,
    Shuffle,
}

public readonly record struct SpotifyPlaybackOrigin(
    string FeatureIdentifier,
    string FeatureVersion,
    string View, string Referrer,
    string DeviceIdentifier,
    string[] FeatureClasses);

/// <summary>
/// TODO
/// </summary>
public readonly record struct SpotifyPlaybackQuality();