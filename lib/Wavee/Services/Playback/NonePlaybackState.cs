using System.Diagnostics;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Player;

namespace Wavee.Services.Playback;

/// <summary>
/// Represents a playback state where no playback is currently active.
/// </summary>
public sealed class NonePlaybackState : ISpotifyPlaybackState
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NonePlaybackState"/>.
    /// </summary>
    public static ISpotifyPlaybackState Instance { get; } = new NonePlaybackState();

    /// <inheritdoc/>
    public DateTimeOffset? PlayingSinceTimestamp { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    // Private constructor to enforce singleton pattern
    private NonePlaybackState()
    {
    }


    public void Update()
    {
    }
}

/// <summary>
/// Represents a playback state where an error has occurred.
/// </summary>
public sealed class ErrorPlaybackState : ISpotifyPlaybackState
{
    /// <summary>
    /// Gets the error message associated with the playback failure.
    /// </summary>
    public string ErrorMessage { get; }

    public DateTimeOffset? PlayingSinceTimestamp { get; } = DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorPlaybackState"/> class with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="errorMessage"/> is <c>null</c> or empty.
    /// </exception>
    public ErrorPlaybackState(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentNullException(nameof(errorMessage), "Error message cannot be null or empty.");

        ErrorMessage = errorMessage;
    }

    public ErrorPlaybackState(Exception errorMessage) : this(errorMessage.Message)
    {
        Exception = errorMessage;
    }

    public Exception? Exception { get; }
}

/// <summary>
/// Represents a regular playback state in Spotify, indicating active playback.
/// </summary>
public abstract record SpotifyRegularPlaybackState : ISpotifyPlaybackState
{
    private readonly TimeSpan _positionSinceSw;
    private protected readonly Stopwatch _stopwatch;

    /// <summary>
    /// Gets the ID of the current track being played.
    /// </summary>
    public SpotifyId CurrentTrackId { get; }

    /// <summary>
    /// Gets the UID of the current track being played.
    /// The UID is usually unique to the current playback context.
    /// </summary>
    public string? CurrentTrackUid { get; }

    /// <summary>
    /// Contains the current track being played.
    /// Note that this property is not guaranteed to be set and may be set at a later time.
    /// </summary>
    public SpotifyPlayableItem? CurrentTrack { get; init; }

    /// <summary>
    /// Gets the total duration of the current track in playback state.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the playback is currently paused.
    /// </summary>
    public bool IsPaused { get; init; }

    /// <summary>
    /// Gets a value indicating whether the playback is currently buffering.
    /// </summary>
    public bool IsBuffering { get; init; }

    /// <summary>
    /// A value indicating the repeat mode of the playback.
    /// </summary>
    public RepeatMode RepeatState { get; init; }

    /// <summary>
    /// A value indicating whether the playback is currently shuffling.
    /// </summary>
    public bool IsShuffling { get; init; }

    /// <summary>
    /// The URL of the context where the current track is being played.
    /// </summary>
    public string ContextUrl { get; }

    /// <summary>
    /// The spotify uri of the context where the current track is being played.
    /// </summary>
    public string ContextUri { get; }

    /// <summary>
    /// The position of the current track in the playback.
    /// </summary>
    public TimeSpan Position => _positionSinceSw + _stopwatch.Elapsed;

    /// <summary>
    /// Gets the identifier of the device where playback is occurring.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets the name of the remote device where playback is occurring.
    /// </summary>
    public string? DeviceName { get; }

    public DateTimeOffset? PlayingSinceTimestamp { get; }

    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyRegularPlaybackState"/> class.
    /// </summary>
    /// <param name="deviceId">The identifier of the device where playback is active.</param>
    /// <param name="deviceName"></param>
    /// <param name="isPaused"></param>
    /// <param name="positionSinceSw"></param>
    /// <param name="stopwatch"></param>
    /// <param name="currentTrackId"></param>
    /// <param name="totalDuration"></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="deviceId"/> is <c>null</c> or empty.
    /// </exception>
    protected SpotifyRegularPlaybackState(string? deviceId, string? deviceName, bool isPaused,
        bool isBuffering,
        TimeSpan positionSinceSw,
        Stopwatch stopwatch, SpotifyId currentTrackId,
        TimeSpan totalDuration,
        DateTimeOffset? playingSinceTimestamp, RepeatMode repeatState, bool isShuffling, string contextUrl,
        string contextUri, string? currentTrackUid, SpotifyPlayableItem? currentTrack)
    {
        IsPaused = isPaused;
        IsBuffering = isBuffering;
        DeviceId = deviceId;
        DeviceName = deviceName;
        _positionSinceSw = positionSinceSw;
        _stopwatch = stopwatch;
        CurrentTrackId = currentTrackId;
        TotalDuration = totalDuration;
        PlayingSinceTimestamp = playingSinceTimestamp;
        RepeatState = repeatState;
        IsShuffling = isShuffling;
        ContextUrl = contextUrl;
        ContextUri = contextUri;
        CurrentTrackUid = currentTrackUid;
        CurrentTrack = currentTrack;
    }
}

/// <summary>
/// Represents a playback state where Spotify is playing on a remote device.
/// </summary>
public sealed record SpotifyRemotePlaybackState : SpotifyRegularPlaybackState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyRemotePlaybackState"/> class.
    /// </summary>
    /// <param name="deviceId">The identifier of the remote device.</param>
    /// <param name="deviceName">The name of the remote device.</param>
    public SpotifyRemotePlaybackState(
        string? deviceId,
        string? deviceName,
        bool isPaused,
        bool isBuffering,
        SpotifyId trackId,
        string? trackUid,
        TimeSpan positionSinceSw,
        Stopwatch stopwatch,
        TimeSpan totalDuration,
        RepeatMode repeatState,
        bool isShuffling,
        string contextUrl,
        string contextUri,
        SpotifyPlayableItem? currentTrack,
        DateTimeOffset clusterTimestamp,
        DateTimeOffset? playingSinceTimestamp,
        string acknowledgmentId
    )
        : base(
            deviceId: deviceId,
            deviceName: deviceName,
            isPaused: isPaused,
            isBuffering: isBuffering,
            positionSinceSw: positionSinceSw,
            stopwatch: stopwatch,
            currentTrackId: trackId,
            totalDuration: totalDuration,
            playingSinceTimestamp: playingSinceTimestamp,
            repeatState: repeatState,
            isShuffling: isShuffling,
            contextUrl: contextUrl,
            contextUri: contextUri,
            currentTrackUid: trackUid,
            currentTrack: currentTrack)
    {
        AcknowledgmentId = acknowledgmentId;
        ClusterTimestamp = clusterTimestamp;
    }

    public string AcknowledgmentId { get; set; }
    public DateTimeOffset ClusterTimestamp { get; }
}

/// <summary>
/// Represents a playback state where Spotify is playing on the current local device.
/// </summary>
public sealed record SpotifyLocalPlaybackState : SpotifyRegularPlaybackState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpotifyLocalPlaybackState"/> class.
    /// </summary>
    /// <param name="playingSince"></param>
    /// <param name="deviceId">The identifier of the local device.</param>
    /// <param name="deviceName"></param>
    /// <param name="isPaused"></param>
    /// <param name="isBuffering"></param>
    /// <param name="trackId"></param>
    /// <param name="positionSinceSw"></param>
    /// <param name="stopwatch"></param>
    /// <param name="totalDuration"></param>
    public SpotifyLocalPlaybackState(DateTimeOffset? playingSince,
        string deviceId,
        string deviceName,
        bool isPaused,
        bool isBuffering,
        SpotifyId trackId,
        string? trackUid,
        TimeSpan positionSinceSw,
        Stopwatch stopwatch,
        TimeSpan totalDuration,
        RepeatMode repeatState,
        bool isShuffling,
        string contextUrl,
        string contextUri,
        SpotifyPlayableItem? currentTrack,
        IReadOnlyDictionary<string, string>? currentTrackMetadata)
        : base(deviceId, deviceName, isPaused, isBuffering, positionSinceSw, stopwatch, trackId, totalDuration,
            playingSince,
            repeatState, isShuffling,
            contextUrl, contextUri,
            trackUid,
            currentTrack)
    {
        CurrentTrackMetadata = currentTrackMetadata;
    }

    public IReadOnlyDictionary<string, string>? CurrentTrackMetadata { get; }
    public Stopwatch Stopwatch => _stopwatch;

    public SpotifyLocalPlaybackState WithNewPosition(TimeSpan newPosition)
    {
        var newStopwatch = IsPaused ? new Stopwatch() : Stopwatch.StartNew();
        return new SpotifyLocalPlaybackState(PlayingSinceTimestamp, DeviceId, DeviceName, IsPaused, IsBuffering,
            CurrentTrackId, CurrentTrackUid, newPosition, newStopwatch, TotalDuration, RepeatState, IsShuffling,
            ContextUrl,
            ContextUri, CurrentTrack, CurrentTrackMetadata);
    }

    public SpotifyLocalPlaybackState WithIsPaused(bool b)
    {
        return this with { IsPaused = b };
    }

    public SpotifyLocalPlaybackState WithTrack(SpotifyPlayableItem? track)
    {
        return this with
        {
            CurrentTrack = track,
            TotalDuration = track?.Duration ?? TimeSpan.Zero,
            IsBuffering = track is null
        };
    }

    public SpotifyLocalPlaybackState WithIsShuffling(bool trackQueueShuffling)
    {
        return this with { IsShuffling = trackQueueShuffling };
    }
    public SpotifyLocalPlaybackState WithRepeatMode(RepeatMode mode)
    {
        return this with { RepeatState = mode };
    }
}