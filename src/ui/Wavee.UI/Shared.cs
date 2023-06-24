using Wavee.Player;

namespace Wavee.UI;

public static class Shared
{
    static Shared()
    {
        Player = new WaveePlayer();
    }
    public static IWaveePlayer Player { get; }
    public static GlobalSettings GlobalSettings { get; set; }
}