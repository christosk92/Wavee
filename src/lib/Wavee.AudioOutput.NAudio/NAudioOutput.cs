using System.Buffers;
using LanguageExt;
using NAudio.Wave;
using Wavee.Core.Infrastructure.Traits;
using static LanguageExt.Prelude;

namespace Wavee.AudioOutput.NAudio;

public sealed class NAudioOutput : AudioOutputIO
{
    private const int sampleRate = 44100;
    private const int channels = 2;
    private static readonly WaveOutEvent _waveOutEvent = new();
    private static readonly NAudioOutput _instance = new();
    private readonly WaveFormat waveFormat;

    private readonly BufferedWaveProvider _bufferedWaveProvider;
    private readonly AtomSeq<nHolder> _outputs = LanguageExt.AtomSeq<nHolder>.Empty;

    public NAudioOutput()
    {
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        _waveOutEvent.Init(_bufferedWaveProvider);
    }

    public static void SetAsMainOutput()
    {
        _ = WaveeCore.Runtime;
        atomic(() => WaveeCore.AudioOutput.Swap(_ => _instance));
    }

    private class nHolder : IDisposable
    {
        public nHolder(Stream stream)
        {
            WaveStream = AudioDecoderRuntime.OpenAudioDecoder(stream).Run().ThrowIfFail();
        }

        public WaveStream? WaveStream { get; set; }

        public void Seek(TimeSpan position)
        {
            if (WaveStream is not null && WaveStream.CanSeek)
            {
                bool succeeded = false;
                while (!succeeded)
                {
                    try
                    {
                        WaveStream.CurrentTime = position;
                        succeeded = true;
                    }
                    catch (Exception ex)
                    {
                        succeeded = false;
                        //try go back a bit
                        if (position.TotalSeconds > 0)
                        {
                            position = position.Subtract(TimeSpan.FromSeconds(0.1));
                        }
                        else
                        {
                            //give up
                            break;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            WaveStream = null;
            WaveStream?.Dispose();
        }
    }

    public Unit Start()
    {
        _waveOutEvent.Play();
        return Prelude.unit;
    }

    public Unit Pause()
    {
        _waveOutEvent.Pause();
        return Prelude.unit;
    }

    // public ValueTask<Unit> WriteSamples(ReadOnlySpan<float> samples, CancellationToken ct = default)
    // {
    //     throw new NotImplementedException();
    // }

    public Task PlayStream(Stream stream, Action<TimeSpan> onPositionChanged, bool closeOtherStreams)
    {
        var output = new nHolder(stream);
        if (closeOtherStreams)
        {
            _outputs.Iter(o => o.Dispose());
        }

        Prelude.atomic(() => _outputs.Swap(x => x.Add(output)));

        var tcs = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task.Factory.StartNew(async () =>
        {
            //read samples
            //Memory<byte> samplesSpan = new byte[1024];
            // byte[] samples = ArrayPool<byte>.Shared.Rent(samplesSpan.Length);

            //read samples
            var samplesSpan = new byte[4096];
            if (output.WaveStream != null)
            {
                while (true)
                {
                    try
                    {
                        int read = output.WaveStream.Read(samplesSpan);
                        if (read > 0)
                        {
                            _bufferedWaveProvider.AddSamples(samplesSpan, 0, read);
                            onPositionChanged(output.WaveStream.CurrentTime);
                            while (_bufferedWaveProvider.BufferedDuration.TotalSeconds > 0.5)
                            {
                                await Task.Delay(5);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        onPositionChanged(output.WaveStream.CurrentTime);
                    }
                }
            }


            tcs.SetResult(Prelude.unit);

            atomic(() => _outputs.Swap(x => x.Filter(f => f != output)));
            output.Dispose();
            onPositionChanged = null;
        });

        return tcs.Task;
    }

    public TimeSpan Position()
    {
        //last one
        return _outputs.LastOrNone().Match(
            Some: o => o.WaveStream?.CurrentTime ?? TimeSpan.Zero,
            None: () => TimeSpan.Zero);
    }

    public Unit Seek(TimeSpan seekPosition)
    {
        //last one
        _waveOutEvent.Pause();
        _outputs.LastOrNone().IfSome(o => o.Seek(seekPosition));
        _bufferedWaveProvider.ClearBuffer();
        _waveOutEvent.Play();
        return Prelude.unit;
    }
}