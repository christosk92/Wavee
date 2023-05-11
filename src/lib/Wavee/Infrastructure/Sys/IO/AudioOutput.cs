using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt.Effects.Traits;
using LibVLCSharp.Shared;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Sys.IO;

public static class AudioOutput<RT>
    where RT : struct, HasCancel<RT>, HasAudioOutput<RT>
{
    /// <summary>
    /// Resume audio playback. Aka resuming.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Start() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.Start())
        select unit;

    /// <summary>
    /// Halt current audio playback. Aka pausing.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Pause() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.Pause())
        select unit;

    /// <summary>
    /// Halt current audio playback. Aka pausing.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> Stop() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.Stop())
        select unit;

    public static Eff<RT, Unit> SetVolume(double volumeFrac) =>
        from _ in default(RT).AudioOutputEff.Map(e => e.SetVolume(Math.Clamp(volumeFrac, 0, 1)))
        select unit;

    /// <summary>
    /// Resume audio playback. Aka resuming.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Task<Unit>> PlayStream(Stream stream, bool closeOtherStreams) =>
        from r in default(RT).AudioOutputEff.Map(e => e.PutStream(stream, closeOtherStreams))
        select r;

    public static Eff<RT, Unit> Seek(TimeSpan pPosition) =>
        from _ in default(RT).AudioOutputEff.Map(e => e.Seek(pPosition))
        select unit;

    public static Eff<RT, Option<TimeSpan>> Position() =>
        from pos in default(RT).AudioOutputEff.Map(e => e.Position)
        select pos;

    public static Eff<RT, double> Volume() =>
        from vol in default(RT).AudioOutputEff.Map(e => e.Volume)
        select vol;
}