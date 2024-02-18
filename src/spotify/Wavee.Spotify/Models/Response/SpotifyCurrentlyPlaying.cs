using System.Diagnostics;
using Wavee.Core;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Interfaces;

namespace Wavee.Spotify.Models.Response;

public sealed class SpotifyCurrentlyPlaying
{
    private TimeSpan _positionOffset;
    private Stopwatch _positionStopwatch;

    public SpotifyCurrentlyPlaying(TimeSpan positionOffset)
    {
        _positionOffset = positionOffset;
        if (_positionOffset < TimeSpan.Zero)
        {
            _positionOffset = TimeSpan.Zero;
        }
        _positionStopwatch = Paused ? new Stopwatch() : Stopwatch.StartNew();
    }

    public SpotifyCurrentlyPlaying(TimeSpan positionOffset, Stopwatch positionStopwatch)
    {
        _positionOffset = positionOffset;
        if (_positionOffset < TimeSpan.Zero)
        {
            _positionOffset = TimeSpan.Zero;
        }

        _positionStopwatch = positionStopwatch;
    }

    public required bool IsPlayingOnThisDevice { get; init; }

    /// <summary>
    /// The id of the device this currently playing item is playing on
    ///
    /// Please note that this string may be empty if the device is not known.
    /// In this case, playback should likely resume on this device. Even though <see cref="IsPlayingOnThisDevice"/> will be false.
    /// </summary>
    public required string DeviceId { get; init; }

    public SpotifyDevice? Device
    {
        get
        {
            if (string.IsNullOrEmpty(DeviceId)) return null;

            if (Devices.TryGetValue(DeviceId, out var device)) return device;

            return null;
        }
    }

    public required ISpotifyPlayableItem? Item { get; init; }
    public required SpotifyContextInfo? Context { get; init; }

    public TimeSpan Position => _positionOffset + (_positionStopwatch?.Elapsed ?? TimeSpan.Zero);

    /// <summary>
    /// A collection of devices that are currently connected to the user's Spotify account.
    ///
    /// This does not include the host device (SpotifyPrivateDevice).
    /// </summary>
    public required IReadOnlyDictionary<string, SpotifyDevice> Devices { get; init; }

    /// <summary>
    /// A boolean that indicates whether a device is playing the track.
    ///
    /// Note that this is a helper property that will be true if <see cref="DeviceId"/> is not empty.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// A boolean that indicates whether the current track is playing.
    ///
    /// Note that this will be true if there is no active device.
    /// </summary>
    public required bool Paused { get; init; }

    /// <summary>
    /// A boolean that indicates whether the current track is playing.
    /// </summary>
    public required bool ShuffleState { get; init; }

    /// <summary>
    /// An enum that indicates the repeat state of the current playback.
    /// </summary>
    public required RepeatState RepeatState { get; init; }
}