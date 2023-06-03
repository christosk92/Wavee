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

    public CrossfadeStream(WaveStream mainStream)
    {
        _mainStream = mainStream;
        _mainSampleProvider = mainStream.ToSampleProvider();
    }

    public TimeSpan CurrentTime
    {
        get => _mainStream.CurrentTime;
        set => _mainStream.CurrentTime = value;
    }

    public bool Ended => _mainStream.Position >= _mainStream.Length;


    public Span<float> ReadSamples(int sampleCount)
    {
        var buffer = new float[sampleCount];
        var read = _mainSampleProvider.Read(buffer, 0, sampleCount);
        if (_crossfadingOut)
        {
            // crossfde duration is like 10 second
            //meaning, at the final 10 seconds of the track, we need to start fading out
            var multiplier = 1f - (float)(_mainStream.CurrentTime.TotalSeconds / _mainStream.TotalTime.TotalSeconds);
            for (var i = 0; i < read; i++)
            {
                buffer[i] *= multiplier;
            }
        }
        else if (_crossfadingIn)
        {
            var multiplier = (float)(_mainStream.CurrentTime.TotalSeconds / _mainStream.TotalTime.TotalSeconds);
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
            static CrossfadeStream OpenDecoder(Stream stream)
            {
                //TODO: check other formats
                var decoder = new Mp3FileReader(stream);
                var wave32 = new WaveChannel32(decoder);

                return new CrossfadeStream(wave32);
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
                var decoder = OpenDecoder(trackStream);
                _mainStream = decoder;
                _positionUpdates.OnNext(_state.Value.StartFrom);

                if (_state.Value.StartFrom.IsSome && _state.Value.StartFrom.ValueUnsafe() > TimeSpan.Zero)
                {
                    SeekTo(decoder, _state.Value.StartFrom.ValueUnsafe());
                    //decoder.CurrentTime = _state.Value.StartFrom.ValueUnsafe();
                }

                var dur = trackDetails.Duration;

                bool goout = false;
                while (true)
                {
                    if (goout)
                    {
                        break;
                    }

                    //read samples from main stream
                    const int sampleCount = 4096;
                    var buffer = decoder.ReadSamples(sampleCount);
                    //if reached end, break
                    if (buffer.Length == 0 || decoder.Ended)
                    {
                        break;
                    }

                    //check if we need to crossfade out
                    if (CrossfadeDuration.IsSome)
                    {
                        if (decoder.CurrentTime > dur - CrossfadeDuration.ValueUnsafe())
                        {
                            //if we are already crossfading out, skip
                            if (_crossfadingOut is not null)
                            {
                                continue;
                            }

                            //if we are not crossfading out, crossfade out
                            Task.Run(async () =>
                            {
                                await SkipNext(true);
                                _crossfadingOut = _mainStream;
                                _crossfadingOut?.CrossfadingOut(CrossfadeDuration.ValueUnsafe());
                                goout = true;
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
                    }

                    var bufferSpan = MemoryMarshal.Cast<float, byte>(buffer);

                    NAudioSink.Instance.Write(bufferSpan);
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

    private async ValueTask SkipNext(bool crossfadeIn)
    {
        if (crossfadeIn)
        {
            var nextState = await atomic(() => _state.SwapAsync(async x => await x.SkipNext()));
            if (nextState.TrackDetails.IsSome)
            {
                _playbackEvent.Set();
            }
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
            TrackDetails = trackStream
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