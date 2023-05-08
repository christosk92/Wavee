using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt.Effects.Traits;
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
    public static Eff<RT, Unit> Stop() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.Stop())
        select unit;

    /// <summary>
    /// Write audio data (in samples form) to the audio device
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Write(ReadOnlyMemory<double> data) =>
        from _ in default(RT).AudioOutputEff.MapAsync(e => e.Write(data))
        select unit;

    /// <summary>
    /// Write audio data (in raw form) to the audio device
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Write(ReadOnlyMemory<byte> data) =>
        from _ in default(RT).AudioOutputEff.MapAsync(e => e.Write(data))
        select unit;

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, Unit> DiscardBuffer() =>
        from _ in default(RT).AudioOutputEff.Map(e => e.DiscardBuffer())
        select unit;
}