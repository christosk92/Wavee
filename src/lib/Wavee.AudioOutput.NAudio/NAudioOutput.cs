using LanguageExt;
using NAudio.Wave;
using Wavee.Core.Infrastructure.Traits;
using static LanguageExt.Prelude;

namespace Wavee.AudioOutput.NAudio;

public sealed class NAudioOutput : AudioOutputIO
{
    private static readonly WaveOutEvent _waveOutEvent = new();
    private static readonly NAudioOutput _instance = new();

    private readonly AtomSeq<nHolder> _outputs = LanguageExt.AtomSeq<nHolder>.Empty;

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
                WaveStream.CurrentTime = position;
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

    public Task PlayStream(Stream stream, Action<TimeSpan> onPositionChanged, bool closeOtherStreams)
    {
        var output = new nHolder(stream);
        if (closeOtherStreams)
        {
            _outputs.Iter(o => o.Dispose());
        }

        Prelude.atomic(() => _outputs.Swap(x => x.Add(output)));

        _waveOutEvent.Init(output.WaveStream);

        var tcs = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task.Factory.StartNew(async () =>
        {
            while (output.WaveStream is not null
                   && (output.WaveStream.CurrentTime < output.WaveStream.TotalTime))
            {
                await Task.Delay(100);
                onPositionChanged(output.WaveStream.CurrentTime);
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
        _outputs.LastOrNone().IfSome(o => o.Seek(seekPosition));
        return Prelude.unit;
    }
}