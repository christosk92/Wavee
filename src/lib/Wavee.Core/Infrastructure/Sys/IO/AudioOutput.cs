using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Wavee.Core.Contracts;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Sys.IO;

public static class AudioOutput<RT>
    where RT : struct, HasCancel<RT>, HasAudioOutput<RT>
{
    /// <summary>
    /// Resume audio playback. Aka resuming.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Start() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.IfSome(x => x.Start()))
        select unit;

    /// <summary>
    /// Halt current audio playback. Aka pausing.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Pause() =>
        from _ in default(RT).AudioOutputEff.Map(e =>
            e.IfSome(x => x.Pause()))
        select unit;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, IAudioDecoder> Decode(IAudioStream stream) =>
        from handle in default(RT).AudioOutputEff.MapAsync(e => e.Match(
            Some: x => x.OpenDecoder(stream),
            None: () => throw new Exception("No audio output device available")))
        select handle;

    public static Eff<RT, AudioOutputIO> GetAudioOutput() =>
        from handle in default(RT).AudioOutputEff.Map(x=> x.Match(Some: r => r, None: () => throw new Exception("No audio output device available")))
        select handle;
    
    public static Eff<RT, Unit> DiscardSamples() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.IfSome(x => x.DiscardSamples()))
        select unit;
}