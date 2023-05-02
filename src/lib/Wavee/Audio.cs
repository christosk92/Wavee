using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;

namespace Wavee;

public static class Audio
{
    private static readonly Runtime Runtime;

    static Audio()
    {
        Runtime = Runtime.New();
    }

    public static async ValueTask<Unit> Write(ReadOnlyMemory<double> data)
    {
        var result = await Audio<Runtime>.Write(data).Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Write failed"));
    }

    public static Unit Start()
    {
        var result = Audio<Runtime>.Start().Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Start failed"));
    }

    public static Unit Stop()
    {
        var result = Audio<Runtime>.Stop().Run(Runtime);
        return result.Match(
            Succ: _ => unit,
            Fail: _ => throw new Exception("Audio.Stop failed"));
    }
}