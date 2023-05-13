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

    public Task PlayStream(Stream stream, Action<TimeSpan> onPositionChanged, bool closeOtherStreams)
    {
        var output = new VlcOutput(stream);
        if (closeOtherStreams)
        {
            _outputs.Iter(o => o.Player.Stop());
        }

        atomic(() => _outputs.Swap(x => x.Add(output)));
        output.Player.Play();
        var tcs = new TaskCompletionSource<Unit>();
        try
        {
            output.Player.PositionChanged += (sender, args) =>
            {
                onPositionChanged(TimeSpan.FromSeconds(args.Position));
            };
            output.Player.Stopped += (sender, args) =>
            {
                atomic(() => _outputs.Swap(x => x.Filter(x => x != output)));
                output.Dispose();
                tcs.SetResult(Unit.Default);
            };
            output.Player.EndReached += (sender, args) =>
            {
                atomic(() => atomic(() => _outputs.Swap(x => x.Filter(x => x != output))));
                output.Dispose();
                tcs.SetResult(Unit.Default);
            };
        }
        catch (Exception e)
        {
            tcs.SetResult(Unit.Default);
        }

        return tcs.Task;
    }

    private class VlcOutput : IDisposable
    {
        public readonly MediaPlayer Player;
        private readonly StreamMediaInput Input;
        public Stream Stream;
        private readonly Media Media;

        public VlcOutput(Stream stream)
        {
            Input = new StreamMediaInput(stream);
            Media = new Media(_libVlc, Input);
            Player = new MediaPlayer(Media);
            Stream = stream;
        }

        public void Dispose()
        {
            Player.Dispose();
            Input.Dispose();
            Media.Dispose();
            Stream = null;
        }
    }
}