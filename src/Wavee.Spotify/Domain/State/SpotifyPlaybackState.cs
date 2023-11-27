using System.Diagnostics;
using Eum.Spotify.connectstate;
using Wavee.Domain.Playback;
using Wavee.Spotify.Domain.Playback;
using Wavee.Spotify.Domain.Remote;

namespace Wavee.Spotify.Domain.State;

/// <summary>
/// Immutable struct that represents the current playback state of the Spotify client.
/// </summary>
/// <param name="IsActive">
///  True if there exists an active playback session, false otherwise. Note that this does not mean that the client is playing music.
///  The client can be paused, or not playing at all, but is still considered active.
/// </param>
/// <param name="Device">
/// The device that is currently active. This is null if <see cref="IsActive"/> is false.
/// </param>
public readonly record struct SpotifyPlaybackState(bool IsActive,
    SpotifyDevice? Device,
    IReadOnlyCollection<SpotifyDevice> OtherDevices,
    SpotifyPlaybackContext Context,
    SpotifyPlaybackOrigin PlayOrigin,
    SpotifyPlaybackTrackInfo? TrackInfo,
    string? PlaybackId,
    string? SessionId,
    bool IsPaused,
    bool Shuffling,
    WaveeRepeatState RepeatState)
{
    public TimeSpan Position => PositionSwOffset + (PositionSw?.Elapsed ?? TimeSpan.Zero);

    internal Stopwatch PositionSw { get; init; }
    internal TimeSpan PositionSwOffset { get; init; }

    internal static SpotifyPlaybackState InActive()
    {
        return new SpotifyPlaybackState(false,
            null,
            OtherDevices: Array.Empty<SpotifyDevice>(),
            Context: default,
            PlayOrigin: default,
            TrackInfo: null,
            PlaybackId: null,
            SessionId: null,
            IsPaused: false,
            Shuffling: false, RepeatState: WaveeRepeatState.None);
    }
}

public readonly record struct SpotifyPlaybackTrackInfo(string Uri, string Uid);