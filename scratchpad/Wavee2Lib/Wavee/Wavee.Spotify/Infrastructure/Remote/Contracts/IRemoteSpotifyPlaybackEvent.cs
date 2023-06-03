using Eum.Spotify.connectstate;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.Remote.Contracts;

public enum RemoteSpotifyPlaybackEventType
{
    Play,
    SeekTo,
    Pause,
    Resume,
    SkipNext,
    UpdateDevice,
    Shuffle,
    Repeat,
    SetQueue,
    AddToQueue
}

public readonly struct RemoteSpotifyPlaybackEvent
{
    public required RemoteSpotifyPlaybackEventType EventType { get; init; }
    public Option<AudioId> TrackId { get; init; }
    public required Option<string> TrackUid { get; init; }
    public required Option<int> TrackIndex { get; init; }
    public Option<ProvidedTrack> PlayingFromQueue { get; init; }
    public TimeSpan PlaybackPosition { get; init; }
    public Option<string> ContextUri { get; init; }
    public Option<bool> IsPaused { get; init; }
    public Option<bool> IsShuffling { get; init; }
    public Option<RepeatState> RepeatState { get; init; }
    public Option<TimeSpan> SeekTo { get; init; }
    public Option<string> SentBy { get; init; }
    public Option<uint> CommandId { get; init; }
    public Option<IEnumerable<ProvidedTrack>> Queue { get; init; }
    public Option<ProvidedTrack> TrackForQueueHint { get; init; }
}