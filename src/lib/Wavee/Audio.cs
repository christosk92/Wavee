using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;

namespace Wavee;

internal sealed record PlaybackHandle(Guid Id,
    Stream Stream,
    Ref<TimeSpan> Position,
    Ref<bool> Close,
    int ChunkSize);

public static class Audio
{
    private static ConcurrentDictionary<Guid, PlaybackHandle> _playbackHandles = new();
    internal static readonly Runtime Runtime;

    static Audio()
    {
        Runtime = Runtime.New();
    }

    public static Guid Play(Stream stream)
    {
        var id = Guid.NewGuid();
        // var position = new Subject<TimeSpan>();
        // var close = new Subject<bool>();
        var pos = Ref(TimeSpan.Zero);
        var close = Ref(false);

        var handle = new PlaybackHandle(id, stream, pos, close, 1024);
        _playbackHandles.TryAdd(id, handle);

        var result = AudioInput<Runtime>.ReadRaw(stream, handle.ChunkSize, pos, close)
            .Run(Runtime);

        _ = Task.Run(async () =>
        {
            var r = result.Match(
                Fail: _ => throw new Exception("Audio.ReadRaw failed"),
                Succ: r => r
            );
            _ = AudioOutput<Runtime>.Start().MapFail(x => throw new Exception("Audio.Start failed")).Run(Runtime);
            await foreach (var payload in r)
            {
                var result = await AudioOutput<Runtime>.Write(payload).Run(Runtime);
                result.Match(
                    Succ: _ => unit,
                    Fail: _ => throw new Exception("Audio.Write failed"));
            }
        });

        return id;
    }

    public static bool Clear(Guid id)
    {
        if (_playbackHandles.TryRemove(id, out var handle))
        {
            atomic(() =>
            {
                handle.Close.Value = true;
                handle.Stream.Dispose();
            });
            return true;
        }

        return false;
    }

    public static bool Pause()
    {
        var result = AudioOutput<Runtime>.Stop().Run(Runtime);
        return result.Match(
            Succ: _ => true,
            Fail: _ => false);
    }

    public static bool Resume()
    {
        var result = AudioOutput<Runtime>.Start().Run(Runtime);
        return result.Match(
            Succ: _ => true,
            Fail: _ => false);
    }


    public static async ValueTask<Unit> Write(ReadOnlyMemory<double> data)
    {
        var result = await AudioOutput<Runtime>.Write(data).Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Write failed"));
    }

    public static Unit Start()
    {
        var result = AudioOutput<Runtime>.Start().Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Start failed"));
    }

    public static Unit Stop()
    {
        var result = AudioOutput<Runtime>.Stop().Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Stop failed"));
    }
}