using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Vorbis;
using NAudio.Wave;
using Wavee.Sinks;
using static LanguageExt.Prelude;

namespace Wavee.Player;

internal class CrossfadeStream : IDisposable
{
    private readonly WaveStream _mainStream;
    private readonly ISampleProvider _mainSampleProvider;

    private TimeSpan _crossfadeDuration;
    private bool _crossfadingOut;
    private bool _crossfadingIn;
    private readonly TimeSpan _duration;

    public CrossfadeStream(WaveStream mainStream, TimeSpan duration)
    {
        _mainStream = mainStream;
        _duration = duration;
        _mainSampleProvider = mainStream.ToSampleProvider();
    }

    public TimeSpan CurrentTime
    {
        get => _mainStream.CurrentTime;
        set => _mainStream.CurrentTime = value;
    }

    public bool Ended => _mainStream.Position >= _mainStream.Length;
    public WaveFormat WaveFormat => _mainStream.WaveFormat;


    public Span<float> ReadSamples(int sampleCount)
    {
        var buffer = new float[sampleCount];
        var read = _mainSampleProvider.Read(buffer, 0, sampleCount);
        if (_crossfadingOut)
        {
            var diffrence = _duration - _mainStream.CurrentTime;
            //if this approaches 0, then 0/(x) -> 0, 
            //if this approaches 10 seconds, and crossfadeDur = 10 seconds, then 10/10 -> 1
            var multiplier = (float)(diffrence.TotalSeconds / _crossfadeDuration.TotalSeconds);
            multiplier = Math.Clamp(multiplier, 0, 1);

            for (var i = 0; i < read; i++)
            {
                buffer[i] *= multiplier;
            }
        }
        else if (_crossfadingIn)
        {
            var difference = _crossfadeDuration - _mainStream.CurrentTime;
            var progress = (float)(difference.TotalSeconds / _crossfadeDuration.TotalSeconds);
            //if diff approaches 0, (meaning we have reached it) then this will result in 0/x -> 0
            //so we need to get the complement of this
            var multiplier = Math.Clamp(progress, 0, 1);
            multiplier = 1 - multiplier;
            for (var i = 0; i < read; i++)
            {
                buffer[i] *= multiplier;
            }
        }

        return buffer;
    }

    public void CrossfadingOut(TimeSpan crossfadeDuration)
    {
        _crossfadeDuration = crossfadeDuration;
        _crossfadingOut = true;
    }

    public void CrossfadeIn(TimeSpan crossfadeDuration)
    {
        _crossfadeDuration = crossfadeDuration;
        _crossfadingIn = true;
    }

    public void Dispose()
    {
        _mainStream.Dispose();
    }
}

public sealed class WaveePlayer
{
    private readonly ManualResetEvent _playbackEvent = new ManualResetEvent(false);
    private readonly Subject<Option<TimeSpan>> _positionUpdates = new();
    private static WaveePlayer _instance;

    private readonly Ref<WaveePlayerState> _state = Ref(WaveePlayerState.Empty());

    //the main track
    private CrossfadeStream? _mainStream;

    //the track being crossfaded out (previous track)
    private CrossfadeStream? _crossfadingOut;

    public static WaveePlayer Instance => _instance ??= new WaveePlayer();

    public WaveePlayer()
    {
        Task.Factory.StartNew(() =>
        {
            static CrossfadeStream OpenDecoder(Stream stream, TimeSpan duration)
            {
                //TODO: check other formats
                stream.Position = 0;

                var decoder = new VorbisWaveReader(stream, duration);
                //var decoder = new Mp3FileReader(stream);
                //var wave32 = new WaveChannel32(decoder);

                return new CrossfadeStream(decoder, duration);
            }

            while (true)
            {
                _playbackEvent.WaitOne();
                if (_state.Value.TrackDetails.IsNone)
                {
                    _playbackEvent.Reset();
                    continue;
                }

                var trackDetails = _state.Value.TrackDetails.ValueUnsafe();
                var trackStream = trackDetails.AudioStream;
                var decoder = OpenDecoder(trackStream, trackDetails.Duration);
                _mainStream = decoder;
                if (_crossfadingOut is not null)
                {
                    _mainStream.CrossfadeIn(CrossfadeDuration.ValueUnsafe());
                }

                _positionUpdates.OnNext(_state.Value.StartFrom);

                if (_state.Value.StartFrom.IsSome && _state.Value.StartFrom.ValueUnsafe() > TimeSpan.Zero)
                {
                    SeekTo(decoder, _state.Value.StartFrom.ValueUnsafe());
                    //decoder.CurrentTime = _state.Value.StartFrom.ValueUnsafe();
                }

                var dur = trackDetails.Duration;

                bool startedCrossfade = false;
                bool goout = false;

                const int sampleCount = 4096;
                var expectedTimeIncreasePerCycle =
                    TimeSpan.FromSeconds((double)sampleCount / decoder.WaveFormat.SampleRate);
                _positionUpdates.OnNext(decoder.CurrentTime);
                while (true)
                {
                    if (goout)
                    {
                        break;
                    }

                    //read samples from main stream
                    var old = decoder.CurrentTime;
                    var buffer = decoder.ReadSamples(sampleCount);
                    var diff = decoder.CurrentTime - old;
                    if (diff > expectedTimeIncreasePerCycle || diff < TimeSpan.Zero)
                    {
                        _positionUpdates.OnNext(decoder.CurrentTime);
                    }

                    //if reached end, break
                    if (buffer.Length == 0 || decoder.Ended)
                    {
                        break;
                    }

                    //check if we need to crossfade out
                    if (CrossfadeDuration.IsSome && !startedCrossfade)
                    {
                        if (decoder.CurrentTime > dur - CrossfadeDuration.ValueUnsafe())
                        {
                            startedCrossfade = true;

                            //if we are already crossfading out, skip
                            if (_crossfadingOut is not null)
                            {
                                continue;
                            }

                            //if we are not crossfading out, crossfade out
                            Task.Run(async () =>
                            {
                                var skipped = await SkipNext(true);
                                if (skipped)
                                {
                                    _crossfadingOut = _mainStream;
                                    _crossfadingOut?.CrossfadingOut(CrossfadeDuration.ValueUnsafe());
                                    goout = true;
                                }
                            });
                        }
                    }

                    //if crossfading out, read samples from crossfading out stream
                    if (_crossfadingOut is not null)
                    {
                        var crossfadeBuffer = _crossfadingOut.ReadSamples(sampleCount);
                        for (var i = 0; i < crossfadeBuffer.Length; i++)
                        {
                            buffer[i] += crossfadeBuffer[i];
                        }

                        if (_crossfadingOut.Ended)
                        {
                            _crossfadingOut.Dispose();
                            _crossfadingOut = null;
                        }
                    }

                    var bufferSpan = MemoryMarshal.Cast<float, byte>(buffer);

                    NAudioSink.Instance.Write(bufferSpan);
                }

                if (!goout)
                {
                    _playbackEvent.Reset();
                    _ = SkipNext(false);
                }
            }
        });
    }

    public void SeekTo(TimeSpan valueUnsafe)
    {
        if (_mainStream is null)
        {
            return;
        }

        SeekTo(_mainStream, valueUnsafe);
    }

    private async ValueTask<bool> SkipNext(bool crossfadeIn)
    {
        if (crossfadeIn)
        {
            var maybe = _state.Value.TrackId.Map(f => f.ToString());
            var currentTrack = _state.Value.TrackUid.IfNone(maybe.IfNone(""));
            
            var next = await atomic(() => _state.SwapAsync(async x =>
            {
                var maybe = _state.Value.TrackId.Map(f => f.ToString());
                var currentTrack = _state.Value.TrackUid.IfNone(maybe.IfNone(""));

                var nextState = await x.SkipNext(false);
                
                var maybeTo = nextState.TrackId.Map(f => f.ToString());
                var nextTrack = nextState.TrackUid.IfNone(maybeTo.IfNone(""));
                
                if (nextTrack != currentTrack)
                {
                    _playbackEvent.Set();
                }
                else
                {
                    _playbackEvent.Reset();
                }

                return nextState;
            }));
            
            var maybeTo = next.TrackId.Map(f => f.ToString());
            var nextTrack = next.TrackUid.IfNone(maybeTo.IfNone(""));
            if (currentTrack != nextTrack)
            {
                return true;
            }

            return false;

            return next.PermanentEnd;
        }
        else
        {
            _mainStream?.Dispose();
            _mainStream = null;
            _crossfadingOut?.Dispose();
            _crossfadingOut = null;

            var next = await atomic(() => _state.SwapAsync(async x =>
            {
                var maybe = _state.Value.TrackId.Map(f => f.ToString());
                var currentTrack = _state.Value.TrackUid.IfNone(maybe.IfNone(""));

                var nextState = await x.SkipNext(true);
                
                var maybeTo = nextState.TrackId.Map(f => f.ToString());
                var nextTrack = nextState.TrackUid.IfNone(maybeTo.IfNone(""));

                if (nextTrack != currentTrack)
                {
                    _playbackEvent.Set();
                }
                else
                {
                    _playbackEvent.Reset();
                }

                return nextState;
            }));

            return !next.PermanentEnd;
        }
    }

    private void SeekTo(CrossfadeStream decoder, TimeSpan valueUnsafe)
    {
        try
        {
            decoder.CurrentTime = valueUnsafe;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            //try again, 100ms back
            SeekTo(decoder, valueUnsafe - TimeSpan.FromMilliseconds(100));
        }
    }

    public async Task Play(WaveeContext context, Option<int> indexInContext, Option<TimeSpan> startFrom,
        bool startPaused, CancellationToken ct = default)
    {
        var track = context.FutureTracks.ElementAtOrDefault(indexInContext.IfNone(0));
        if (track is null)
        {
            return;
        }

        var trackStream = await track.Factory(ct);

        atomic(() => _state.Swap(x => x with
        {
            TrackId = track.TrackId,
            TrackUid = track.TrackUid,
            Context = Some(context),
            IsPaused = false,
            IsShuffling = false,
            RepeatState = x.RepeatState switch
            {
                RepeatState.Context => RepeatState.Context,
                RepeatState.None => RepeatState.None,
                RepeatState.Track => RepeatState.Context
            },
            StartFrom = startFrom,
            TrackDetails = trackStream,
            PermanentEnd = false
        }));
        if (startPaused)
        {
            NAudioSink.Instance.Pause();
        }
        else
        {
            NAudioSink.Instance.Resume();
        }

        _playbackEvent.Set();
    }


    public IObservable<WaveePlayerState> StateUpdates => _state.OnChange().StartWith(_state.Value);
    public IObservable<Option<TimeSpan>> PositionUpdates => _positionUpdates.StartWith(Position);

    public WaveePlayerState State => _state.Value;
    public Option<TimeSpan> CrossfadeDuration { get; set; } = Option<TimeSpan>.None;
    public Option<TimeSpan> Position => _mainStream?.CurrentTime ?? Option<TimeSpan>.None;

    public void Resume() => NAudioSink.Instance.Resume();
    public void Pause() => NAudioSink.Instance.Pause();
}