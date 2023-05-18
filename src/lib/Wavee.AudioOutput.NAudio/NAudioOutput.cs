using LanguageExt;
using Wavee.Core.Player;

namespace Wavee.AudioOutput.NAudio;

public static class NAudioOutput
{
    public static Unit SetAsDefaultOutput()
    {
        WaveePlayer.Output.IfSome(x => x.Dispose());
        WaveePlayer.Output = new WaveeNAudioOutput();
        return default;
    }
}