using LanguageExt;
using LanguageExt.UnitsOfMeasure;
using LanguageExt.UnsafeValueAccess;
using Wavee.Player.Ctx;

namespace Wavee.Player.State;

public readonly record struct WaveePlayerState(
    WaveePlaybackStateType State,
    Option<string> SessionId,
    Option<string> PlaybackId,
    Option<string> TrackId,
    Option<WaveeContext> Context,
    Option<WaveeTrack> Track,
    int Index,
    TimeSpan StartFrom,
    bool Shuffling,
    RepeatState RepeatState)
{
    public Option<TimeSpan> Position => Option<TimeSpan>.None;

    public async Task<WaveePlayerState> SkipNext()
    {
        var currentIndex = this.Index;
        var currentContext = this.Context.ValueUnsafe();
        var next = currentContext.FutureTracks.Skip(currentIndex + 1).HeadOrNone();
        if (next.IsNone)
        {
            return this with
            {
                State = WaveePlaybackStateType.PermanentEndOfContext
            };
        }
        
        var nextTrack = await next.ValueUnsafe().Factory(CancellationToken.None);
        return this with
        {
            State = WaveePlaybackStateType.Playing,
            Context = currentContext,
            Index = currentIndex + 1,
            Track = nextTrack,
            TrackId = nextTrack.Id
        };
    }

    public WaveePlayerState PlayContext(WaveeContext playContext,
        int playIndex,
        Option<TimeSpan> playStartFrom,
        Option<bool> playShuffling,
        Option<RepeatState> playRepeatState,
        FutureWaveeTrack? futureWaveeTrack)
    {
        return this with
        {
            State = WaveePlaybackStateType.Loading,
            Context = playContext,
            SessionId = Guid.NewGuid().ToString(),
            Index = playIndex,
            StartFrom = playStartFrom.IfNone(TimeSpan.Zero),
            Shuffling = playShuffling.IfNone(this.Shuffling),
            RepeatState = playRepeatState.IfNone(this.RepeatState),
            TrackId = futureWaveeTrack?.TrackId ?? Option<string>.None
        };
    }

    public WaveePlayerState PermanentEnd()
    {
        return this with
        {
            State = WaveePlaybackStateType.PermanentEndOfContext
        };
    }

    public WaveePlayerState Playing(WaveeTrack waveeTrack, Option<string> playbackId)
    {
        return this with
        {
            State = WaveePlaybackStateType.Playing,
            Track = waveeTrack,
            PlaybackId = playbackId.IfNone(this.PlaybackId.IfNone(string.Empty))
        };
    }
}

public enum WaveePlaybackStateType
{
    Loading,
    Playing,
    Paused,
    PermanentEndOfContext,
}