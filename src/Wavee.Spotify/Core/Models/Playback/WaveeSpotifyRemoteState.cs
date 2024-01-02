using System.Collections.Immutable;
using System.Diagnostics;
using Tango.Types;
using Wavee.Core.Models;

namespace Wavee.Spotify.Core.Models.Playback;

public readonly record struct WaveeSpotifyRemoteState
{
    public required Option<SpotifySimpleContextItem> Item { get; init; }
    public required Option<SpotifySimpleContext> Context { get; init; }

    public TimeSpan Position => PositionStopwatch.Elapsed + PositionOffset;
    public required bool IsPaused { get; init; }
    public required WaveeRepeatStateType RepeatState { get; init; }
    public required bool IsShuffling { get; init; }
    public required IReadOnlyDictionary<SpotifyRestrictionAppliesForType, ImmutableArray<Either<string, SpotifyKnownRestrictionType>>> Restrictions { get; init; }
    internal Stopwatch PositionStopwatch { get; init; }
    internal TimeSpan PositionOffset { get; init; }
}
public enum SpotifyKnownRestrictionType
{
    NotPaused,
    EndlessContext,
    Dj,
    Narration,
    NoPreviousTrack,
    Radio,
    AutoPlay,
}

public enum SpotifyRestrictionAppliesForType
{
    Shuffle,
    SkippingNext,
    SkippingPrevious,
    RepeatContext,
    RepeatTrack,
    Pausing,
    Resuming,
    Seeking,
}