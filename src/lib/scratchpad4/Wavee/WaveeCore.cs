using Wavee.Infrastructure.Live;
using Wavee.Player;

namespace Wavee;

public static class WaveeCore
{
    static WaveeCore()
    {
        Runtime = WaveeRuntime.New();
        Player = new WaveePlayer<WaveeRuntime>(Runtime);
    }

    public static WaveeRuntime Runtime { get; }

    public static IWaveePlayer Player { get; }
}