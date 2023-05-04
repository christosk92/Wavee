using Wavee.Infrastructure.Live;

namespace Wavee;

public static class WaveeCore
{
    static WaveeCore()
    {
        Runtime = WaveeRuntime.New();
    }

    public static WaveeRuntime Runtime { get; }
}