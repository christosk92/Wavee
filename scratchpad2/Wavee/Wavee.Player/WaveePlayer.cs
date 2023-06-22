using System.Buffers;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Serilog;
using Wavee.Player.Command;
using Wavee.Player.Ctx;
using Wavee.Player.Decoding;
using Wavee.Player.State;
using static LanguageExt.Prelude;

namespace Wavee.Player;

public sealed class WaveePlayer : IWaveePlayer
{
    private readonly ChannelWriter<IWaveePlaybackCommand> _writer;

    private readonly Ref<Option<WaveePlayerState>> _state = Ref(Option<WaveePlayerState>.None);

    public WaveePlayer()
    {
        var main = Channel.CreateUnbounded<IWaveePlaybackCommand>();
        _writer = main.Writer;
        var internalPlayer = new PlayerInternal();
        Task.Factory.StartNew(async () =>
        {
            try
            {
                await foreach (var command in main.Reader.ReadAllAsync())
                {
                    await OnPlaybackEvent(command, internalPlayer);
                }
            }
            finally
            {
                main.Writer.Complete();
            }
        });
    }

    public IObservable<Option<WaveePlayerState>> CreateListener() => _state.OnChange().StartWith(_state);
    public Option<WaveePlayerState> CurrentState => _state.Value;

    public ValueTask Play(WaveeContext ctx, int idx, Option<TimeSpan> startFrom, bool startPaused,
        Option<bool> shuffling, Option<RepeatState> repeatState,
        Ref<Option<TimeSpan>> crossfadeDuration)
    {
        var command = IWaveePlaybackCommand.Play(
            Context: ctx,
            Index: idx,
            StartFrom: startFrom,
            StartPaused: startPaused,
            Shuffling: shuffling,
            RepeatState: repeatState,
            CrossfadeDuration: crossfadeDuration
        );
        return _writer.WriteAsync(command);
    }

    public ValueTask SkipNext(bool crossfadein, Ref<Option<TimeSpan>> crossfadeDuration)
    {
        var command = IWaveePlaybackCommand.SkipNext(
            crossfadeIn: crossfadein,
            crossfadeDuration: crossfadeDuration
        );

        return _writer.WriteAsync(command);
    }

    private async Task OnPlaybackEvent(IWaveePlaybackCommand command, PlayerInternal player)
    {
        try
        {
            switch (command)
            {
                case WaveePlaybackSkipNextCommand skipNext:
                {
                    var currentStateOption = _state.Value;
                    if (currentStateOption.IsNone)
                    {
                        return;
                    }

                    var currentState = currentStateOption.ValueUnsafe();
                    var nextState = await currentState.SkipNext();

                    var track = currentState.Track;
                    atomic(() => _state.Swap(_ => nextState));
                    if (track.IsNone)
                    {
                        Log.Warning("No track to skip to");
                        return;
                    }

                    var stream = track.ValueUnsafe();
                    player.Play(stream, skipNext.CrossfadeIn, skipNext.CrossfadeDuration,
                        () => { SkipNext(true, skipNext.CrossfadeDuration).AsTask().Wait(); });
                    player.Resume();

                    break;
                }
                case WaveePlaybackPlayCommand play:
                {
                    var currentStateOption = _state.Value;
                    if (currentStateOption.IsNone)
                    {
                        currentStateOption = new WaveePlayerState();
                    }

                    var currentState = currentStateOption.ValueUnsafe();


                    var track = play.Context.FutureTracks.ElementAtOrDefault(play.Index) ??
                                play.Context.FutureTracks.ElementAtOrDefault(0);
                    var nextState = currentState.PlayContext(play.Context,
                        play.Index,
                        play.StartFrom,
                        play.Shuffling,
                        play.RepeatState,
                        track);

                    atomic(() => _state.Swap(_ => nextState));

                    if (track is null)
                    {
                        //set permanent end state
                        atomic(() => _state.Swap(_ => nextState.PermanentEnd()));
                        return;
                    }

                    var stream = await track.Factory(CancellationToken.None);
                    atomic(() => _state.Swap(_ => nextState.Playing(stream, Guid.NewGuid().ToString())));
                    player.Play(stream, false, play.CrossfadeDuration.Value,
                        () => { SkipNext(true, play.CrossfadeDuration).AsTask().Wait(); });
                    if (play.StartPaused)
                    {
                        player.Pause();
                    }
                    else
                    {
                        player.Resume();
                    }

                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while handling playback event. Going to next track.");
        }
    }
}

internal sealed class PlayerInternal
{
    private record CurrentTrackStream
        (WaveeTrack Track, IAudioDecoder Decoder) : IDisposable
    {
        private TimeSpan _crossfadeDuration = TimeSpan.Zero;
        private bool _crossfadingOut = false;
        private bool _crossfadingIn = false;

        public void Dispose()
        {
            Track.Dispose();
            Decoder.Dispose();
        }
    }

    private SemaphoreSlim _streamLock = new(1, 1);
    private Option<CurrentTrackStream> _currentTrackStream = None;
    private Option<CurrentTrackStream> _crossfadingInStream = None;

    private readonly IWavePlayer _wavePlayer;
    private readonly WaveFormat waveFormat;
    private readonly BufferedWaveProvider _bufferedWaveProvider;

    public PlayerInternal()
    {
        const int sampleRate = 44100;
        const int channels = 2;
        _wavePlayer = new WaveOutEvent();
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _wavePlayer.Init(_bufferedWaveProvider);
        _wavePlayer.Volume = 1;
    }

    public void Play(WaveeTrack stream, bool crossfadingIn, Option<TimeSpan> crossfadeDuration,
        Action crossfadeCallback)
    {
        IAudioDecoder? decoder = null;
        bool didCrossfade = false;
        if (!crossfadingIn || crossfadeDuration.IsNone)
        {
            _currentTrackStream.IfSome(x => x.Dispose());
        }
        else if (crossfadeDuration.IsSome && crossfadingIn)
        {
            didCrossfade = true;

            _currentTrackStream.IfSome(x => x.Decoder.MarkForCrossfadeOut(crossfadeDuration.ValueUnsafe()));
            var crossfadeInDecoder = AudioDecoderFactory.CreateDecoder(stream.AudioStream, stream.Duration);
            _crossfadingInStream = new CurrentTrackStream(stream, crossfadeInDecoder);
            _crossfadingInStream.IfSome(x => x.Decoder.MarkForCrossfadeIn(crossfadeDuration.ValueUnsafe()));
            decoder = crossfadeInDecoder;
        }

        _streamLock.Wait();
        try
        {
            //if crossfading in, swap the current track stream with the crossfading in stream
            if (didCrossfade)
            {
                _currentTrackStream = _crossfadingInStream;
                _crossfadingInStream = None;
            }
            else
            {
                _currentTrackStream = None;
                decoder = AudioDecoderFactory.CreateDecoder(stream.AudioStream, stream.Duration);
                _currentTrackStream = new CurrentTrackStream(stream, decoder);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while playing track.");
        }
        finally
        {
            _streamLock.Release();
        }

        if (decoder != null)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _streamLock.WaitAsync();
                    StreamTrack(decoder, stream.NormalisationData, crossfadeDuration, crossfadeCallback);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error while streaming track.");
                }
                finally
                {
                    _streamLock.Release();
                }
            });
        }
    }

    private void StreamTrack(IAudioDecoder decoder, Option<NormalisationData> normalisationData,
        Option<TimeSpan> crossfadeDuration,
        Action crossfadeCallback)
    {
        //Start reading samples
        bool startedCrossfadeOut = false;
        Span<float> buffer = stackalloc float[decoder.SampleSize];
        while (true)
        {
            if (crossfadeDuration.IsSome && !startedCrossfadeOut)
            {
                //Crossfade out can either start from external (marking the decoder)
                //or by us
                //if the decoder is marked for crossfade out, we can start crossfading out
                if (decoder.IsMarkedForCrossfadeOut)
                {
                    startedCrossfadeOut = true;
                }
                else
                {
                    //if the decoder is not marked for crossfade out, we can mark it IF we have a next track
                    //or else we are gonna crossfade out to nothing
                    bool reachedCrossfadeTime()
                    {
                        var time = decoder.CurrentTime;
                        var duration = decoder.TotalTime;
                        var crossfade = crossfadeDuration.ValueUnsafe();
                        return time >= duration - crossfade;
                    }

                    if (reachedCrossfadeTime())
                    {
                        crossfadeCallback();
                        startedCrossfadeOut = true;
                    }
                }
            }

            var read = decoder.Read(buffer);
            if (read == 0)
            {
                break;
            }

            //normalise
            if (normalisationData.IsSome)
            {
                var (trackGainDb, trackPeak, albumGainDb, albumPeak) = normalisationData.ValueUnsafe();
                var trackGain = MathF.Pow(10, (float)(trackGainDb / 20));
                var albumGain = MathF.Pow(10, (float)(albumGainDb / 20));
                var gain = trackGain * albumGain;
                for (var i = 0; i < read; i++)
                {
                    buffer[i] *= gain;
                }

                //TODO: Peak
            }

            //cast to byte
            ReadOnlySpan<byte> samplesSpan = MemoryMarshal.Cast<float, byte>(buffer.Slice(0, read));
            var samples = ArrayPool<byte>.Shared.Rent(samplesSpan.Length);
            try
            {
                samplesSpan.CopyTo(samples);

                _bufferedWaveProvider.AddSamples(samples, 0, samplesSpan.Length);

                while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
                {
                    Thread.Sleep(1);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(samples);
            }
        }
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        _wavePlayer.Play();
    }
}

public interface IWaveePlayer
{
    IObservable<Option<WaveePlayerState>> CreateListener();
    Option<WaveePlayerState> CurrentState { get; }

    ValueTask Play(WaveeContext ctx, int idx, Option<TimeSpan> startFrom, bool startPaused, Option<bool> shuffling,
        Option<RepeatState> repeatState, Ref<Option<TimeSpan>> crossfadeDuration);

    ValueTask SkipNext(bool crossfadein, Ref<Option<TimeSpan>> crossfadeDuration);
}