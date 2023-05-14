using LanguageExt;
using LibVLCSharp.Shared;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;
using static LanguageExt.Prelude;

namespace Wavee.AudioOutput.LibVLC;

public sealed class LibVlcOutput : AudioOutputIO
{
    private static readonly LibVLCSharp.Shared.LibVLC _libVlc = new();
    private static readonly LibVlcOutput _instance = new();

    private readonly AtomSeq<VlcOutput> _outputs = LanguageExt.AtomSeq<VlcOutput>.Empty;

    public static void SetAsMainOutput()
    {
        _ = WaveeCore.Runtime;
        atomic(() => WaveeCore.AudioOutput.Swap(_ => _instance));
    }

    public Unit Start()
    {
        _outputs.LastOrNone().IfSome(o => o.Player.Play());
        return Unit.Default;
    }

    public Unit Pause()
    {
        _outputs.LastOrNone().IfSome(o => o.Player.Pause());
        return Unit.Default;
    }

    public ValueTask<Unit> WriteSamples(ReadOnlySpan<float> samples, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task PlayStream(Stream stream, Action<TimeSpan> onPositionChanged, bool closeOtherStreams)
    {
        var output = new VlcOutput(stream);
        if (closeOtherStreams)
        {
            _outputs.Iter(o => o.Player.Stop());
        }

        atomic(() => _outputs.Swap(x => x.Add(output)));
        output.Player.Play();
        var tcs = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            void PlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
            {
                onPositionChanged(TimeSpan.FromSeconds(e.Position));
            }

            void Stopped(object? sender, EventArgs e)
            {
                output.Player.Stopped -= Stopped;
                output.Player.PositionChanged -= PlayerOnPositionChanged;
                output.Player.EndReached -= EndReached;

                tcs.SetResult(Unit.Default);
                output.Dispose();
                onPositionChanged = null;
            }

            void EndReached(object? sender, EventArgs _)
            {
                try
                {
                    output.Player.Stopped -= Stopped;
                    output.Player.PositionChanged -= PlayerOnPositionChanged;
                    output.Player.EndReached -= EndReached;

                    atomic(() => atomic(() => _outputs.Swap(x => x.Filter(x => x != output))));
                    tcs.SetResult(Unit.Default);
                    output.Dispose();
                    onPositionChanged = null;
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }

            output.Player.PositionChanged += PlayerOnPositionChanged;
            output.Player.Stopped += Stopped;
            output.Player.EndReached += EndReached;
        }
        catch (Exception e)
        {
            tcs.SetResult(Unit.Default);
        }

        return tcs.Task;
    }

    public TimeSpan Position()
    {
        return _outputs.LastOrNone().Match(
            Some: o => TimeSpan.FromMilliseconds(o.Player.Time),
            None: () => TimeSpan.Zero);
    }

    public Unit Seek(TimeSpan seekPosition)
    {
        _outputs.LastOrNone().IfSome(o => o.Player.SeekTo(seekPosition));
        return Unit.Default;
    }

    private class VlcOutput : IDisposable
    {
        public MediaPlayer Player;
        private StreamMediaInput Input;
        public Stream Stream;
        private Media Media;

        public VlcOutput(Stream stream)
        {
            Input = new StreamMediaInput(stream);
            Media = new Media(_libVlc, Input);
            Player = new MediaPlayer(Media);
            Stream = stream;
        }

        public void Dispose()
        {
            Task.Run(() => { Player.Dispose(); });
            Input.Dispose();
            Media.Dispose();
            Player = null;
            Media = null;
            Input = null;
            Stream = null;
        }
    }
}